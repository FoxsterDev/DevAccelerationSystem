#include <iostream>

extern "C" {
    class Base {
    public:
        virtual void DoSomething() {
            printf("[Native]: Base::DoSomething called\n");
        }
    };

    // Function to cause a crash via null virtual call
    void CauseNullVirtualCallCrash() {
        Base* nullObject = nullptr; // Null pointer
        nullObject->DoSomething(); // Call virtual function on null pointer
    }
}
