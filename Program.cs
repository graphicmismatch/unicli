using System.ComponentModel;
using System.Data;
using System.Net.Mime;

namespace unicli;
using System;
using System.Reflection;

class Program
{
    private static unicli unicliInst;
    static void Main(string[] args)
    {
        unicliInst = new unicli();
        if (args.Length == 0)
        {
            unicliInst.help();
            return;
        }

        MethodInfo? method = unicliInst.GetType().GetMethod(args[0].ToLower());
        if (method is null)
        {
            unicliInst.help();
            return;
        }

        ArgContext ctx;
        if (args.Length == 1)
        {
             ctx = new ArgContext();
        }
        else
        {
            ctx = new ArgContext(args[1..]);
        }

        object[] parameters = [ ctx ];
        ReturnObject? res = (ReturnObject?) method.Invoke(unicliInst, parameters);
        if (res is not null && (res?.Code != ReturnCode.Empty && res?.Code != ReturnCode.Success))
        {
            if (res?.Code == ReturnCode.SuccessWithMessage)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Command Succeeded With Message:\n\t"+(string?)res?.ReturnData);
                Console.ResetColor();
            }
            else if (res?.Code == ReturnCode.SuccessWithWarning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Command Succeeded With Warning:\n\t" + (string?)res?.ReturnData);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Command failed with the following Error:\n\t"+(string?)res?.ReturnData);
                Console.ResetColor();
            }
        }


    }
}