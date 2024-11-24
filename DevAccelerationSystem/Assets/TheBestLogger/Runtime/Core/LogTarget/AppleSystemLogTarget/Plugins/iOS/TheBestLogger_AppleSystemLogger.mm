#import <os/log.h>

os_log_t theBestLogger_appleSystemLogger;  // Declare a global os_log_t variable
NSString *theBestLogger_currentCategory;  // Keep track of the current category

extern "C" {
    // Function to initialize the logger with a custom subsystem and category
    void TheBestLogger_initAppleSystemLogger(const char* subsystem, const char* category)
    {
        NSString *nsSubsystem = [NSString stringWithUTF8String:subsystem];
        theBestLogger_currentCategory = [NSString stringWithUTF8String:category];

        // Initialize the custom log object
        theBestLogger_appleSystemLogger = os_log_create([nsSubsystem UTF8String], [theBestLogger_currentCategory UTF8String]);
    }

    void TheBestLogger_AppleSystemLogDefault(const char* category, const char* message)
    {
        os_log(theBestLogger_appleSystemLogger, "[%s] %{public}s", category, message);  // Default log level
    }

    void TheBestLogger_AppleSystemLogInfo(const char* category, const char* message)
    {
        os_log_info(theBestLogger_appleSystemLogger, "[%s][Info] %{public}s", category, message);  // Info log level
    }

    void TheBestLogger_AppleSystemLogDebug(const char* category, const char* message)
    {
        os_log_debug(theBestLogger_appleSystemLogger, "[%s][Debug] %{public}s", category, message);  // Debug log level
    }

    void TheBestLogger_AppleSystemLogError(const char* category, const char* message)
    {
        os_log_error(theBestLogger_appleSystemLogger, "[%s][Error] %{public}s", category, message);  // Error log level
    }

    void TheBestLogger_AppleSystemLogFault(const char* category, const char* message)
    {
        os_log_fault(theBestLogger_appleSystemLogger, "[%s][Fault] %{public}s", category, message);  // Fault log level
    }

    void TheBestLogger_AppleSystemLogFormatted(const char* format, const char* arg1)
    {
        NSString *formattedString = [NSString stringWithFormat:[NSString stringWithUTF8String:format], [NSString stringWithUTF8String:arg1]];
        os_log(theBestLogger_appleSystemLogger, "[%{public}@] %@", theBestLogger_currentCategory, formattedString);  // Log with formatted message
    }
}
