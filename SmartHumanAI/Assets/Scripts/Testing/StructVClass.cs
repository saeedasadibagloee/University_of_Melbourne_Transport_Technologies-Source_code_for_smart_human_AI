using System.Diagnostics;
using UnityEngine;

namespace Testing
{
    public class StructVClass : MonoBehaviour
    {

        // Update is called once per frame
        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                UnityEngine.Debug.Log("Starting... ");
                Test3();
            }
        }

        public struct foo
        {
            public foo(double arg) { this.y = arg; }
            public double y;
        }
        public class bar
        {
            public bar(double arg) { this.y = arg; }
            public double y;
        }

        static void Test()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 50000000; i++)
            { foo test = new foo(3.14); }
            UnityEngine.Debug.Log("50M structs took " + stopwatch.ElapsedMilliseconds.ToString());

            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 50000000; i++)
            { bar test2 = new bar(3.14); }
            UnityEngine.Debug.Log("50M classes took " + stopwatch.ElapsedMilliseconds.ToString());
        }

        static void Test2()
        {
            string s = "monkeys!";
            int dummy = 0;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(s);
            for (int i = 0; i < 10000000; i++)
                sb.Append(s);
            s = sb.ToString();

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000000; i++)
                dummy++;
            UnityEngine.Debug.Log("10M loop took " + stopwatch.ElapsedMilliseconds.ToString());

            dummy = 0;

            stopwatch = Stopwatch.StartNew();
            foreach (char c in s)
                dummy++;
            UnityEngine.Debug.Log("10M foreach took " + stopwatch.ElapsedMilliseconds.ToString());

            return;
        }

        static void Test3()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000000; i++)
                CalculateNumbers();
            UnityEngine.Debug.Log("CalculateNumbers took " + stopwatch.ElapsedMilliseconds.ToString());

            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000000; i++)
                CalculateNumbers2();
            UnityEngine.Debug.Log("CalculateNumbers2 took " + stopwatch.ElapsedMilliseconds.ToString());
        }

        static float y = 0.123423f;
        static float x = 32690f;
        static float res = 0f;
        static float distance = 0f;

        static void CalculateNumbers()
        {
            distance = x - y;
            res = distance * y;
            res = distance * x;
        }

        static void CalculateNumbers2()
        {
            res = (x - y) * y;
            res = (x - y) * x;
        }
    }
}

