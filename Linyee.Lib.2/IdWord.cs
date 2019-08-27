using System;
using System.Collections.Generic;
using System.Text;

[assembly: CLSCompliant(true)]
namespace Linyee.Lib.V2
{
    /// <summary>
    /// 尝试自写一个编译器
    /// </summary>
	[CLSCompliantAttribute(true)]//主要用于不区分大小写
    public partial class Linyee
    {
        /// <summary>
        /// 标识符
        /// 标示符用于定义一个变量，函数获取其他用户定义的项。标示符以一个字母 A 到 Z 或 a 到 z 或下划线 _ 开头后加上0个或多个字母，下划线，数字（0到9）。
        /// </summary>
        public class IdWord
        {
            /// <summary>
            /// 标识符
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// 关键词
            /// </summary>
            public static string[] LY_Word = new string[] {
                //lua
                "and",	"break",	"do",	"else",
                "elseif",  "end", "false", "for",
                "function",  "if",   "in" ,   "local",
                "nil" ,"not" ,"or",  "repeat",
                "return",    "then" ,   "true" ,   "until",
                "while"
            }; 
        }
    }
}
