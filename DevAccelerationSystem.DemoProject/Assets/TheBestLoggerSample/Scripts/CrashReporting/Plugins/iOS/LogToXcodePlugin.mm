#import <Foundation/Foundation.h>

extern "C" {
void LogToXcode(const char* message) {
NSLog(@"[Unity iOS Plugin]: %s", message);

}
int DivideNumbers(int a, int b) {

return a / b;
}

void NullPointerException() {
NSLog(@"[Unity iOS Plugin]: Triggering NullPointerException...");
int *nullPointer = NULL;
*nullPointer = 42;  // Dereferencing a null pointer will crash
}

void WrappedNullPointerException() {
NSLog(@"[Unity iOS Plugin]: Triggering NullPointerException...");
int *nullPointer = NULL;
*nullPointer = 42;  // Dereferencing a null pointer will crash
}

void TriggerNullReferenceException() {
@try {
NSLog(@"[Unity iOS Plugin]: Triggering Null Reference Exception...");
NSString *nullString = nil;
[nullString length]; // Will throw an exception
}
@catch (NSException *exception) {
NSLog(@"[Unity iOS Plugin]: Caught an exception: %@", exception.reason);
}
@finally {
NSLog(@"[Unity iOS Plugin]: Finished attempting null reference operation.");
}
}

void DispatchedTriggerNullReferenceException() {
NSLog(@"[Unity iOS Plugin]: Triggering Null Reference Exception...");
dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
@try {
NSString *nullString = nil;
[nullString length]; // Will throw an exception
}
@catch (NSException *exception) {
NSLog(@"[Unity iOS Plugin]: Caught an exception: %@", exception.reason);
}
@finally {
NSLog(@"[Unity iOS Plugin]: Finished attempting null reference operation.");
}
});
}

// A function pointer type to be passed from Unity
typedef void (*UnityCallback)(void);

// Executes the given Unity callback on a background thread
void RunOnBackgroundThread(UnityCallback callback) {
dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
if (callback != NULL) {
callback();
}
});
}


}
