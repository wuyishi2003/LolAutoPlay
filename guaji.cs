using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;


namespace LOL自动挂机
{
    public class Hook
    {
        static bool IsStartThread = false;
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        public class SetPaint
        {
            public int X;
            public int Y;
            public int rows;
        }
        [Flags]
        enum MouseEventFlag : uint
        {
            Move = 0x0001,
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            XDown = 0x0080,
            XUp = 0x0100,
            Wheel = 0x0800,
            VirtualDesk = 0x4000,
            Absolute = 0x8000
        }

        //委托 
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        static int hHook = 0;
        public const int WH_KEYBOARD_LL = 13;
        //释放按键的常量
        private const int KEYEVENTF_KEYUP = 2;
        //LowLevel键盘截获，如果是WH_KEYBOARD＝2，并不能对系统键盘截取，Acrobat Reader会在你截取之前获得键盘。 
        static HookProc KeyBoardHookProcedure;

        #region 调用API
        //设置钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        //抽调钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        //调用下一个钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        //获得模块句柄
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetModuleHandle(string name);

        //寻找目标进程窗口
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //查找子窗体
        [DllImport("user32.dll", EntryPoint = "FindWindowEX")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent,
            IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


        //设置进程窗口到最前
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        //模拟键盘事件
        [DllImport("User32.dll")]
        public static extern void keybd_event(Byte bVk, Byte bScan, Int32 dwFlags, Int32 dwExtraInfo);

        //设置鼠标位置
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        //模拟鼠标按键
        [DllImport("user32.dll")]
        static extern void mouse_event(MouseEventFlag flsgs, int dx, int dy, uint data, UIntPtr extraInfo);
        #endregion
        /// <summary>
        /// 安装钩子
        /// </summary>
        public void Hook_Start()
        {
            //安装钩子
            if (hHook == 0)
            {
                KeyBoardHookProcedure = new HookProc(KeyBoatdHookProc);
                hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure,
                        IntPtr.Zero, 0);
                if (hHook == 0) Hook_Clear(); //hook设置失败
            }
        }

        /// <summary>
        /// 卸载Hook
        /// </summary>
        public static void Hook_Clear()
        {
            bool retKeyboard = true;
            if (hHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
        }

        public static int KeyBoatdHookProc(int nCode, int wParam, IntPtr lParam)
        {
            Thread thread1 = new Thread(StartCursor);
            SetPaint sp = new SetPaint();
            sp.X = Screen.PrimaryScreen.Bounds.Width;
            sp.Y = Screen.PrimaryScreen.Bounds.Height;
            sp.rows = 0;
            //监控用户键盘输入
            KeyBoardHookStruct input = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
            Keys k = (Keys)Enum.Parse(typeof(Keys), input.vkCode.ToString());

            if (input.vkCode == (int)Keys.Control || input.vkCode == (int)Keys.Shift || input.vkCode == (int)Keys.F1)
            {
                thread1.IsBackground = true;
                IsStartThread = true;
                thread1.Start(sp);
            }
            else if (input.vkCode == (int)Keys.Control || input.vkCode == (int)Keys.Shift || input.vkCode == (int)Keys.F2)
            {
                Hook_Clear();
                if (null != thread1)
                {
                    thread1.Abort();
                    IsStartThread = false;
                }
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        static void StartCursor(object list)
        {
            SetPaint spaint = list as SetPaint;
            int sWhith = spaint.X;
            int sHeight = spaint.Y;
            int dx = 0;
            int dy = 0;

            while (IsStartThread)
            {
                if (3 < spaint.rows) spaint.rows = 0;
                switch (spaint.rows)
                {
                    case 0:
                        dx = sWhith / 3;
                        dy = sHeight / 3;
                        break;
                    case 1:
                        dy = dy * 2;
                        break;
                    case 2:
                        dx = dx * 2;
                        break;
                    case 3:
                        dy = dy / 2;
                        break;
                    default:
                        break;
                }
                spaint.rows++;
                //MessageBox.Show("width:"+sWhith+" height:"+sHeight+ " X:" + dx + " Y:" + dy+" rows:"+spaint.rows);
                SetCursorPos(dx, dy);
                mouse_event(MouseEventFlag.RightDown | MouseEventFlag.RightUp, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(10000);
            }
        }


    }
}
