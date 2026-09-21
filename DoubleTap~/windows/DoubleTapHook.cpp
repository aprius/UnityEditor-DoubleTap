#include <windows.h>

// key: 0 = Shift, 1 = Control, 2 = Alt, 3 = Windows
// key == -1 signals any other input, which cancels a pending double-tap.
typedef void (*WorldDoubleTapCallback)(int key, int isDown, double time);

static HHOOK gHook = NULL;
static WorldDoubleTapCallback gCallback = NULL;
static bool gDown[4] = { false, false, false, false };
static LARGE_INTEGER gFrequency = { 0 };

static double Now()
{
    LARGE_INTEGER counter;
    QueryPerformanceCounter(&counter);
    return (double)counter.QuadPart / (double)gFrequency.QuadPart;
}

static int MapKey(WPARAM virtualKey)
{
    switch (virtualKey)
    {
        case VK_SHIFT: case VK_LSHIFT: case VK_RSHIFT: return 0;
        case VK_CONTROL: case VK_LCONTROL: case VK_RCONTROL: return 1;
        case VK_MENU: case VK_LMENU: case VK_RMENU: return 2;
        case VK_LWIN: case VK_RWIN: return 3;
        default: return -1;
    }
}

static LRESULT CALLBACK KeyboardProc(int code, WPARAM wParam, LPARAM lParam)
{
    if (code == HC_ACTION && gCallback != NULL)
    {
        const int key = MapKey(wParam);
        const bool isUp = (lParam & (LPARAM)0x80000000) != 0;

        if (key < 0)
        {
            if (!isUp) gCallback(-1, 1, Now());
        }
        else if (isUp)
        {
            if (gDown[key])
            {
                gDown[key] = false;
                gCallback(key, 0, Now());
            }
        }
        else if (!gDown[key])
        {
            gDown[key] = true;
            gCallback(key, 1, Now());
        }
    }

    return CallNextHookEx(NULL, code, wParam, lParam);
}

extern "C" __declspec(dllexport) void WorldDoubleTapHook_Uninstall(void)
{
    if (gHook != NULL)
    {
        UnhookWindowsHookEx(gHook);
        gHook = NULL;
    }

    gCallback = NULL;
}

// Must be called from the thread that pumps the editor message loop.
extern "C" __declspec(dllexport) void WorldDoubleTapHook_Install(WorldDoubleTapCallback callback)
{
    WorldDoubleTapHook_Uninstall();
    if (callback == NULL) return;

    QueryPerformanceFrequency(&gFrequency);
    gCallback = callback;
    for (int i = 0; i < 4; i++) gDown[i] = false;

    gHook = SetWindowsHookExW(WH_KEYBOARD, KeyboardProc, NULL, GetCurrentThreadId());
}
