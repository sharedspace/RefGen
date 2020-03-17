using System;

namespace RefGen
{
    public partial class A 
    { 
        static A()
        {
            _threadStaticValue = true;
        }

        public A()
        {
            _good = false;
        }

        public void Test() 
        {
            ProtectedTest();
        } 

        protected void ProtectedTest()
        {

        }

        public bool Good
        {
            get
            {
                return _good;
            }

            private set
            {
                _good = value;
            }
        }

        internal static bool ThreadStaticValue
        {
            get
            {
                return _threadStaticValue;
            }
        }

        public bool IsGreen 
        {
            get
            {
                return _b.IsGreen;
            }
        }

        private bool _good;

        [ThreadStatic]
        private static bool _threadStaticValue;

        private B _b = new B();
    }
}