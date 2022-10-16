using System;

namespace ChatRoom.Utils
{
    internal static class SimpleLogger
    {
        #region Info
        public static void Info()
        {
            Console.WriteLine();
        }
        public static void Info(bool value)
        {
            Console.WriteLine(value);
        }
        public static void Info(char value)
        {
            Console.WriteLine(value);
        }
        public static void Info(char[] buffer)
        {
            Console.WriteLine(buffer);
        }
        public static void Info(char[] buffer, int index, int count)
        {
            Console.WriteLine(buffer, index, count);
        }
        public static void Info(decimal value)
        {
            Console.WriteLine(value);
        }
        public static void Info(double value)
        {
            Console.WriteLine(value);
        }
        public static void Info(float value)
        {
            Console.WriteLine(value);
        }
        public static void Info(int value)
        {
            Console.WriteLine(value);
        }
        public static void Info(uint value)
        {
            Console.WriteLine(value);
        }
        public static void Info(long value)
        {
            Console.WriteLine(value);
        }
        public static void Info(ulong value)
        {
            Console.WriteLine(value);
        }
        public static void Info(object value)
        {
            Console.WriteLine(value);
        }
        public static void Info(string value)
        {
            Console.WriteLine(value);
        }
        public static void Info(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }
        public static void Info(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }
        public static void Info(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }
        public static void Info(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        {
            ArgIterator argIterator = new ArgIterator(__arglist);
            int num = argIterator.GetRemainingCount() + 4;
            object[] array = new object[num];
            array[0] = arg0;
            array[1] = arg1;
            array[2] = arg2;
            array[3] = arg3;
            for (int i = 4; i < num; i++)
            {
                array[i] = TypedReference.ToObject(argIterator.GetNextArg());
            }
            Console.WriteLine(format, array);
        }
        public static void Info(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
        #endregion
        #region Error
        public static void Error()
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.ForegroundColor = temp;
        }
        public static void Error(bool value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(char value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(char[] buffer)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(buffer);
            Console.ForegroundColor = temp;
        }
        public static void Error(char[] buffer, int index, int count)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(buffer, index, count);
            Console.ForegroundColor = temp;
        }
        public static void Error(decimal value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(double value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(float value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(int value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(uint value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(long value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(ulong value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(object value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(string value)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(value);
            Console.ForegroundColor = temp;
        }
        public static void Error(string format, object arg0)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg0);
            Console.ForegroundColor = temp;
        }
        public static void Error(string format, object arg0, object arg1)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg0, arg1);
            Console.ForegroundColor = temp;
        }
        public static void Error(string format, object arg0, object arg1, object arg2)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg0, arg1, arg2);
            Console.ForegroundColor = temp;
        }
        public static void Error(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        {
            ArgIterator argIterator = new ArgIterator(__arglist);
            int num = argIterator.GetRemainingCount() + 4;
            object[] array = new object[num];
            array[0] = arg0;
            array[1] = arg1;
            array[2] = arg2;
            array[3] = arg3;
            for (int i = 4; i < num; i++)
            {
                array[i] = TypedReference.ToObject(argIterator.GetNextArg());
            }
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, array);
            Console.ForegroundColor = temp;
        }
        public static void Error(string format, params object[] arg)
        {
            ConsoleColor temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg);
            Console.ForegroundColor = temp;
        }
        #endregion
    }
}