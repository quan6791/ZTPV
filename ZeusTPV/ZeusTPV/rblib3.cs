using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ZeusTPV
{
    internal class rblib3
    {
    }

    public class Robot
    {
        // Buffer size
        private const int N_RCVBUF = 1024;

        // Command codes
        private const int NOP = 1;
        private const int SVSW = 2;
        private const int PLSMOVE = 3;
        private const int MTRMOVE = 4;
        private const int JNTMOVE = 5;
        private const int PTPMOVE = 6;
        private const int CPMOVE = 7;
        private const int SET_TOOL = 8;
        private const int CHANGE_TOOL = 9;
        private const int ASYNCM = 10;
        private const int PASSM = 11;
        private const int OVERLAP = 12;
        private const int MARK = 13;
        private const int JMARK = 14;
        private const int IOCTRL = 15;
        private const int ZONE = 16;
        private const int J2R = 17;
        private const int R2J = 18;
        private const int SYSSTS = 19;
        private const int TRMOVE = 20;
        private const int ABORTM = 21;
        private const int JOINM = 22;
        private const int SLSPEED = 23;
        private const int ACQ_PERMISSION = 24;
        private const int REL_PERMISSION = 25;
        private const int SET_MDO = 26;
        private const int ENABLE_MDO = 27;
        private const int DISABLE_MDO = 28;
        private const int PMARK = 29;
        private const int VERSION = 30;
        private const int ENCRST = 31;
        private const int SAVEPARAMS = 32;
        private const int CALCPLSOFFSET = 33;
        private const int SET_LOG_LEVEL = 34;
        private const int FSCTRL = 35;
        private const int PTPPLAN = 36;
        private const int CPPLAN = 37;
        private const int PTPPLAN_W_SP = 38;
        private const int CPPLAN_W_SP = 39;
        private const int SYSCTRL = 40;
        private const int OPTCPMOVE = 41;
        private const int OPTCPPLAN = 42;
        private const int PTPMOVE_MT = 43;
        private const int MARK_MT = 44;
        private const int J2R_MT = 45;
        private const int R2J_MT = 46;
        private const int PTPPLAN_MT = 47;
        private const int PTPPLAN_W_SP_MT = 48;
        private const int JNTRMOVE = 49;
        private const int JNTRMOVE_WO_CHK = 50;
        private const int CPRMOVE = 51;
        private const int CPRPLAN = 52;
        private const int CPRPLAN_W_SP = 53;
        private const int SUSPENDM = 54;
        private const int RESUMEM = 55;
        private const int GETMT = 56;
        private const int RELBRK = 57;
        private const int CLPBRK = 58;
        private const int ARCMOVE = 59;
        private const int CIRMOVE = 60;
        private const int MMARK = 61;

        private string _host;
        private int _port;
        private TcpClient _sock;
        private NetworkStream _stream;
        private object _lock = new object();
        private bool _linearJointSupport = false;

        public Robot(string host, int port)
        {
            _host = host;
            _port = port;
            _sock = null;
            _stream = null;
            _linearJointSupport = false;
        }

        ~Robot()
        {
            Close();
        }

        public bool Open()
        {
            if (_sock == null)
            {
                _sock = new TcpClient();
                _sock.Connect(_host, _port);
                _stream = _sock.GetStream();
            }

            var ver = Version();
            if (ver == null || (ver.Length > 0 && ver[0].ToString() == "False"))
            {
                throw new System.Exception("Error! Unable to open rblib.");
            }
            else
            {
                if (Convert.ToInt32(ver[1]) >= 1 || Convert.ToInt32(ver[2]) >= 8)
                {
                    _linearJointSupport = true;
                }
                else
                {
                    _linearJointSupport = false;
                }
            }
            return true;
        }

        public void Close()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
            if (_sock != null)
            {
                _sock.Close();
                _sock = null;
            }
        }


        // Methods for sending command and receiving response from the robot
        private object[] ChkRes(int cmdId, string buf)
        {
            if (string.IsNullOrEmpty(buf))
            {
                Console.WriteLine("chkRes buffer empty");
                return new object[] { false, 99, 1 }; // Transaction error , buffer empty
            }

            var jsonObj = JsonSerializer.Deserialize<JsonElement>(buf);
            int cmd = jsonObj.GetProperty("cmd").GetInt32();
            if (cmdId != cmd)
            {
                // Nhận lại dữ liệu để kiểm tra một lần nữa
                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string retryBuf = Encoding.ASCII.GetString(recvBuf, 0, len);
                jsonObj = JsonSerializer.Deserialize<JsonElement>(retryBuf);
                if (cmdId != jsonObj.GetProperty("cmd").GetInt32())
                {
                    Console.WriteLine("chkRes cmd ID error");
                    return new object[] { false, 99, 1 }; // Transaction error , cmd ID error
                }
            }

            var results = jsonObj.GetProperty("results");
            if (results.ValueKind == JsonValueKind.Array)
            {
                var arr = new object[results.GetArrayLength()];
                int i = 0;
                foreach (var item in results.EnumerateArray())
                {
                    arr[i++] = item.ToString();
                }

                return arr;
            }
            else
            {
                return new object[] { results.ToString() };
            }
        }

        // Check version
        public object[] Version()
        {
            var paramObj = new
            {
                cmd = VERSION,
                @params = new object[] { }

            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                // Send command to the robot
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                // Receive response from the robot
                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                // Process the response
                return ChkRes(VERSION, buf);
            }
        }

        public object[] PlsMove(int ax1, int ax2, int ax3, int ax4, int ax5, int ax6, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = PLSMOVE,
                @params = new object[]
                {
                    new int[] {ax1, ax2, ax3, ax4, ax5, ax6 },
                    speed,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(VERSION, buf);
            }
        }

        public object[] MtrMove(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, double speed, double acct, double dacct)
        {
            double[] axes = new double[]
            {
                ax1* Math.PI / 180.0,
                ax2* Math.PI / 180.0,
                ax3* Math.PI / 180.0,
                ax4* Math.PI / 180.0,
                ax5* Math.PI / 180.0,
                ax6* Math.PI / 180.0
            };

            var paramObj = new
            {
                cmd = MTRMOVE,
                @params = new object[]
                {
                    axes,
                    speed,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(MTRMOVE, buf);
            }
        }

        public object[] JntMove(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, double speed, double acct, double dacct)
        {
            object axes;
            if (_linearJointSupport)
            {
                axes = new double[] { ax1, ax2, ax3, ax4, ax5, ax6 };
            }
            else
            {
                axes = new double[]
                {
                    ax1 * Math.PI / 180.0,
                    ax2 * Math.PI / 180.0,
                    ax3 * Math.PI / 180.0,
                    ax4 * Math.PI / 180.0,
                    ax5 * Math.PI / 180.0,
                    ax6 * Math.PI / 180.0
                };
            }

            var paramObj = new
            {
                cmd = JNTMOVE,
                @params = new object[]
                {
                    axes,
                    speed,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(JNTMOVE, buf);
            }
        }

        public object[] AcqPermission()
        {
            var paramObj = new
            {
                cmd = ACQ_PERMISSION,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(ACQ_PERMISSION, buf);
            }

        }
    }

}
