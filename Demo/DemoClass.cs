using System;

namespace Demo
{
    public class DemoClass
    {
        public void ParameterlessProcedure()
        {

        }

        public int ParameterlessFunction()
        {
            return 1;
        }

        public void Procedure(int number, DemoClass demoObject)
        {

        }

        public int Function(int number, DemoClass demoObject)
        {
            return 2;
        }

        public int FunctionWithRef(int number, DemoClass demoObject, ref int dummy)
        {
            return 2;
        }

        public int FunctionWithOut(int number, DemoClass demoObject, out int dummy)
        {
            dummy = 1;
            return 2;
        }

        public int GenericFunction<TParam>(TParam genericParameter, int number, DemoClass demoObject)
        {
            return 2;
        }

        protected int ProtectedFunction(int number, DemoClass demoObject)
        {
            return 2;
        }

        public int Property
        {
            get; set;
        }

        protected DemoClass ProtectedProperty
        {
            get; set;
        }
    }
}
