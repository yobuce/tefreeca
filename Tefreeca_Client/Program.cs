using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Tefreeca_Structure;

namespace Tefreeca_Client
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Set_flag(args);
            Application.Run(new TefreecaClient());
        }

        private static void Set_flag(string[] arg) //배열로 부터 설정을 받아 세팅한다.
        {
            foreach (string ar in arg)
            {
                if (ar.StartsWith("-"))
                {
                    switch (ar.ToLower().Trim())
                    {
                        case "-debug": //디버그 모드
                            {
                                Data_structure.is_debug = true;
                                Console.WriteLine("Debug Mode.");
                            }
                            break;
                        default:
                            {
                                Console.WriteLine("Option Error : {0}", ar);
                                return;
                            }
                    }
                }
                else
                {
                    Console.WriteLine("Option Error : {0}", ar);
                    return;
                }
            }
        }
    }
}
