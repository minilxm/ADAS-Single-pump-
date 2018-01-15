/*
 * System.Reflection 命名空间
 * Type type = Type.GetType("类的完全限定名"); 
 * object obj = type.Assembly.CreateInstance(type); 
 * 可以根据类对象的限定名来反射创建实例，所以不用担心无法访问派生类对象
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cmd
{
    /// <summary>
    /// 命令工厂，用户不用关心命令是怎么创建的。
    /// </summary>
    public class CommandFactory
    {
        public static BaseCommand CreateCommand(byte messageID)
        {
            BaseCommand cmd = null;
            switch (messageID)
            {
                case 0x01:
                    cmd = new CmdUploadSN();
                    break;
                case 0x02:
                    cmd = new CmdSendPumpType();
                    break;
                case 0x0003:
                    cmd = new CmdCharge();
                    break;
                case 0x0004:
                    cmd = new CmdDischarge();
                    break;
                case 0x0005:
                    cmd = new CmdRecharge();
                    break;
                case 0x0006:
                    cmd = new CmdFinishAging();
                    break;
                case 0x0007:
                    cmd = new GetAlarm();
                    break;
                case 0x0008:
                    cmd = new CmdGetPumpStatus();
                    break;
                default:
                    cmd = null;
                    break;
            }
            return cmd;
        }
    }
}
