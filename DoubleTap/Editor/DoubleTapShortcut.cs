#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    public enum ModifierKey
    {
        Shift = 0,
        Control = 1,
        Alt = 2,
        Command = 3
    }

    /// <summary>
    /// Fires an action when a modifier key is tapped twice in a row, the way JetBrains IDEs open
    /// Search Everywhere on double Shift. Unity's own event pipeline cannot express this: IMGUI
    /// emits no key-up for modifiers and drops events while a field, a modal or a compile owns the
    /// editor. Detection therefore runs in a native editor plugin hooked into the platform event
    /// queue, in-process, so it only ever sees input while the editor is focused.
    /// </summary>
    [InitializeOnLoad]
    public static class DoubleTapShortcut
    {
        private const string LIBRARY = "DoubleTapHook";
        private const int KEY_COUNT = 4;

#pragma warning disable UDR0001
        /// <summary>Longest a tap may be held before it stops counting as a tap.</summary>
        public static double MaxHoldSeconds { get; set; } = 0.3;

        /// <summary>Longest gap allowed between the first release and the second press.</summary>
        public static double MaxGapSeconds { get; set; } = 0.3;
#pragma warning restore UDR0001

        private enum Phase
        {
            Idle,
            FirstDown,
            FirstUp,
            SecondDown
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeEvent(int key, int isDown, double time);

        private static readonly Phase[] Phases = new Phase[KEY_COUNT];
        private static readonly double[] Stamps = new double[KEY_COUNT];
        private static readonly Action[] Callbacks = new Action[KEY_COUNT];
        private static readonly Queue<int> Fired = new Queue<int>();
        private static readonly NativeEvent Handler = OnNativeEvent;

        static DoubleTapShortcut()
        {
            if (!Install()) return;

#pragma warning disable UDR0001
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;
            EditorApplication.update += Drain;
#pragma warning restore UDR0001
        }

        public static void Register(ModifierKey key, Action action)
        {
            Callbacks[(int)key] += action;
        }

        public static void Unregister(ModifierKey key, Action action)
        {
            Callbacks[(int)key] -= action;
        }

        private static bool Install()
        {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
            try
            {
                WorldDoubleTapHook_Install(Handler);
                return true;
            }
            catch (DllNotFoundException)
            {
                Debug.LogWarning($"[DoubleTapShortcut] Native plugin '{LIBRARY}' not found, double-tap shortcuts are disabled. Build it from Assets/World/Core/Editor/DoubleTap~.");
                return false;
            }
#else
            return false;
#endif
        }

        private static void Shutdown()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            EditorApplication.quitting -= Shutdown;
            EditorApplication.update -= Drain;

#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
            WorldDoubleTapHook_Uninstall();
#endif
        }

        // Runs inside the platform event dispatch; an escaping exception would unwind into native code.
        [MonoPInvokeCallback(typeof(NativeEvent))]
        private static void OnNativeEvent(int key, int isDown, double time)
        {
            try
            {
                if (key < 0 || key >= KEY_COUNT)
                {
                    Array.Clear(Phases, 0, KEY_COUNT);
                    return;
                }

                if (isDown != 0) OnDown(key, time);
                else OnUp(key, time);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void OnDown(int key, double time)
        {
            for (var i = 0; i < KEY_COUNT; i++)
            {
                if (i != key) Phases[i] = Phase.Idle;
            }

            var chained = Phases[key] == Phase.FirstUp && time - Stamps[key] <= MaxGapSeconds;
            Phases[key] = chained ? Phase.SecondDown : Phase.FirstDown;
            Stamps[key] = time;
        }

        private static void OnUp(int key, double time)
        {
            var held = time - Stamps[key];
            var phase = Phases[key];
            Phases[key] = Phase.Idle;

            if (held > MaxHoldSeconds) return;

            if (phase == Phase.FirstDown)
            {
                Phases[key] = Phase.FirstUp;
                Stamps[key] = time;
            }
            else if (phase == Phase.SecondDown)
            {
                Fired.Enqueue(key);
            }
        }

        // Deferred out of the native callback so handlers can touch editor windows safely.
        private static void Drain()
        {
            while (Fired.Count > 0) Callbacks[Fired.Dequeue()]?.Invoke();
        }

#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
        [DllImport(LIBRARY)]
        private static extern void WorldDoubleTapHook_Install(NativeEvent callback);

        [DllImport(LIBRARY)]
        private static extern void WorldDoubleTapHook_Uninstall();
#endif
    }
}

#endif