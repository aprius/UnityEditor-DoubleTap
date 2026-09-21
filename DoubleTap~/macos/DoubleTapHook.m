#import <Cocoa/Cocoa.h>

// key: 0 = Shift, 1 = Control, 2 = Alt/Option, 3 = Command
// key == -1 signals any other input, which cancels a pending double-tap.
typedef void (*WorldDoubleTapCallback)(int key, int isDown, double time);

static const NSUInteger kModifierFlags[4] = {
    NSEventModifierFlagShift,
    NSEventModifierFlagControl,
    NSEventModifierFlagOption,
    NSEventModifierFlagCommand
};

static id gFlagsMonitor = nil;
static id gInputMonitor = nil;
static WorldDoubleTapCallback gCallback = NULL;
static NSUInteger gPreviousFlags = 0;

void WorldDoubleTapHook_Uninstall(void)
{
    if (gFlagsMonitor != nil)
    {
        [NSEvent removeMonitor:gFlagsMonitor];
        gFlagsMonitor = nil;
    }

    if (gInputMonitor != nil)
    {
        [NSEvent removeMonitor:gInputMonitor];
        gInputMonitor = nil;
    }

    gCallback = NULL;
}

void WorldDoubleTapHook_Install(WorldDoubleTapCallback callback)
{
    WorldDoubleTapHook_Uninstall();
    if (callback == NULL) return;

    gCallback = callback;
    gPreviousFlags = [NSEvent modifierFlags] & NSEventModifierFlagDeviceIndependentFlagsMask;

    gFlagsMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskFlagsChanged
                                                          handler:^NSEvent *(NSEvent *event)
    {
        NSUInteger current = [event modifierFlags] & NSEventModifierFlagDeviceIndependentFlagsMask;
        NSUInteger changed = current ^ gPreviousFlags;
        gPreviousFlags = current;

        if (gCallback != NULL)
        {
            double time = [event timestamp];
            for (int i = 0; i < 4; i++)
            {
                if ((changed & kModifierFlags[i]) == 0) continue;
                gCallback(i, (current & kModifierFlags[i]) != 0 ? 1 : 0, time);
            }
        }

        return event;
    }];

    NSEventMask inputMask = NSEventMaskKeyDown | NSEventMaskLeftMouseDown | NSEventMaskRightMouseDown | NSEventMaskOtherMouseDown;
    gInputMonitor = [NSEvent addLocalMonitorForEventsMatchingMask:inputMask
                                                          handler:^NSEvent *(NSEvent *event)
    {
        if (gCallback != NULL) gCallback(-1, 1, [event timestamp]);
        return event;
    }];
}
