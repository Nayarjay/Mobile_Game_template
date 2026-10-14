// Retour haptique court (appelé par GameCore.Haptics).
// strength : 0 = léger, 1 = moyen, 2 = fort.
#import <UIKit/UIKit.h>

extern "C" void GameHapticImpact(int strength)
{
    if (@available(iOS 10.0, *))
    {
        UIImpactFeedbackStyle style = UIImpactFeedbackStyleLight;
        if (strength == 1) style = UIImpactFeedbackStyleMedium;
        else if (strength >= 2) style = UIImpactFeedbackStyleHeavy;

        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        [generator prepare];
        [generator impactOccurred];
    }
}
