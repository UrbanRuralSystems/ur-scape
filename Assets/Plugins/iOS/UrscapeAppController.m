#import <UIKit/UIKit.h>
#import "UnityAppController.h"

@interface UrscapeAppController : UnityAppController
{
}

- (BOOL) application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions;
- (void) openURLAfterDelay:(NSURL*) url;
-(BOOL) application:(UIApplication*)application handleOpenURL:(NSURL*)url;
-(BOOL) application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation;
@end

@implementation UrscapeAppController

- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
	[super application:application didFinishLaunchingWithOptions:launchOptions];

	if ([launchOptions objectForKey:UIApplicationLaunchOptionsURLKey])
	{
		NSURL *url = [launchOptions objectForKey:UIApplicationLaunchOptionsURLKey];
		[self performSelector:@selector(openURLAfterDelay:) withObject:url afterDelay:4];
	}

	return YES;
}

- (void) openURLAfterDelay:(NSURL*) url
{
    UnitySendMessage("UrlSchemeHandler", "HandleUrl", [[url absoluteString] UTF8String]);
}

-(BOOL) application:(UIApplication*)application handleOpenURL:(NSURL*)url
{
    UnitySendMessage("UrlSchemeHandler", "HandleUrl", [[url absoluteString] UTF8String]);
    return YES;
}

-(BOOL) application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation
{
    UnitySendMessage("UrlSchemeHandler", "HandleUrl", [[url absoluteString] UTF8String]);
    return YES;
}
@end

IMPL_APP_CONTROLLER_SUBCLASS(UrscapeAppController)
