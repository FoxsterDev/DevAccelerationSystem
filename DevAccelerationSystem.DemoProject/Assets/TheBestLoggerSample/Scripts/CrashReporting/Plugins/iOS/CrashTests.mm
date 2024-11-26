#import <Foundation/Foundation.h>
#import <stdlib.h>
#import <pthread.h>

extern "C" {

// 1. Dereference a null pointer
void CauseNullPointerCrash() {
int *nullPointer = NULL;
*nullPointer = 42;
}

// 2. Access array out of bounds
void CauseArrayBoundsCrash() {
int arr[5] = {1, 2, 3, 4, 5};
int invalidAccess = arr[10];
}

// 3. Divide by zero
void CauseDivideByZeroCrash() {
int a = 10;
int b = 0;
int c = a / b;
}

// 4. Unaligned memory access
void CauseUnalignedMemoryAccessCrash() {
char data[4] = {0, 0, 0, 0};
int *unalignedPointer = (int *)(data + 1);
*unalignedPointer = 42;
}

// 5. Infinite recursion
void CauseStackOverflowCrash() {
CauseStackOverflowCrash(); // Recursive call
}

// 6. Double free
void CauseDoubleFreeCrash() {
int *ptr = (int *)malloc(sizeof(int));
free(ptr);
free(ptr); // Double free
}

// 7. Accessing freed memory
void CauseUseAfterFreeCrash() {
int *ptr = (int *)malloc(sizeof(int));
free(ptr);
*ptr = 42; // Use after free
}

// 8. Illegal instruction
void CauseIllegalInstructionCrash() {
__builtin_trap(); // Illegal instruction
}

// 9. Invalid memory access
void CauseInvalidMemoryAccessCrash() {
int *invalidPointer = (int *)0xDEADBEEF;
*invalidPointer = 42;
}

// 10. Throwing an uncaught exception
void CauseUncaughtExceptionCrash() {
@throw [NSException exceptionWithName:@"TestException"
reason:@"This is an uncaught exception"
userInfo:nil];
}

// 11. Invalid function pointer
void CauseInvalidFunctionPointerCrash() {
void (*invalidFunctionPointer)() = (void (*)())0xDEADBEEF;
invalidFunctionPointer();
}

// 12. Buffer overflow
void CauseBufferOverflowCrash() {
char buffer[10];
for (int i = 0; i < 20; i++) { // Write beyond buffer size
buffer[i] = 'A';
}
}

// 13. Mutex crash
void CauseMutexCrash() {
pthread_mutex_t mutex;
pthread_mutex_unlock(&mutex); // Unlocking uninitialized mutex
}

// 14. Null virtual call (C++ only, skip for Objective-C)

// 15. Trigger abort()
void CauseAbortCrash() {
abort();
}

// 16. Trigger NullReferenceNSException()
void NullReferenceNSException() {
NSLog(@"[Unity iOS Plugin]: Triggering Null Reference Exception...");
@throw [NSException exceptionWithName:@"NullReferenceException"
reason:@"This is a forced null reference exception"
userInfo:nil];
}
// 16. Trigger InvalidFileHandleException()
void InvalidFileHandleException() {
NSFileHandle *handle = [NSFileHandle fileHandleForReadingAtPath:@"invalid/path"];
[handle readDataToEndOfFile]; // Invalid file handle, throws NSFileHandleOperationException
}

// 17.Mallformed JsonParsing()

typedef void (*UnityCallback)(const char* message);

void ParseMalformedJSON(const char *jsonString, UnityCallback callback) {
NSString *json = [NSString stringWithUTF8String:jsonString];
NSData *data = [json dataUsingEncoding:NSUTF8StringEncoding];
NSDictionary *parsedJSON = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
NSString *successMessage = @"JSON parsed successfully.";
callback([successMessage UTF8String]);
}





void ParseJSONOnBackgroundThread(const char *jsonString, UnityCallback callback) {
NSString *json = [NSString stringWithUTF8String:jsonString];

dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
@try {
NSData *data = [json dataUsingEncoding:NSUTF8StringEncoding];
NSDictionary *parsedJSON = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
if (parsedJSON != nil) {
NSString *successMessage = @"JSON parsed successfully.";
callback([successMessage UTF8String]);
} else {
NSString *errorMessage = @"Failed to parse JSON.";
callback([errorMessage UTF8String]);
}
}
@catch (NSException *exception) {
NSString *exceptionMessage = [NSString stringWithFormat:@"Exception: %@", exception.reason];
callback([exceptionMessage UTF8String]);
}
});
}

}
