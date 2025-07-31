using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
//using System.Text.Json;

namespace ZeusTPV
{
    public static class RblibVersion
    {
        public static int[] Version()
        {
            return new int[] { 0, 3, 1, 0 };
        }
    }

    public class Robot
    {
        // Buffer size
        private const int _N_RCVBUF = 1024;

        // Command codes
        private const int _NOP = 1;
        private const int _SVSW = 2;
        private const int _PLSMOVE = 3;
        private const int _MTRMOVE = 4;
        private const int _JNTMOVE = 5;
        private const int _PTPMOVE = 6;
        private const int _CPMOVE = 7;
        private const int _SET_TOOL = 8;
        private const int _CHANGE_TOOL = 9;
        private const int _ASYNCM = 10;
        private const int _PASSM = 11;
        private const int _OVERLAP = 12;
        private const int _MARK = 13;
        private const int _JMARK = 14;
        private const int _IOCTRL = 15;
        private const int _ZONE = 16;
        private const int _J2R = 17;
        private const int _R2J = 18;
        private const int _SYSSTS = 19;
        private const int _TRMOVE = 20;
        private const int _ABORTM = 21;
        private const int _JOINM = 22;
        private const int _SLSPEED = 23;
        private const int _ACQ_PERMISSION = 24;
        private const int _REL_PERMISSION = 25;
        private const int _SET_MDO = 26;
        private const int _ENABLE_MDO = 27;
        private const int _DISABLE_MDO = 28;
        private const int _PMARK = 29;
        private const int _VERSION = 30;
        private const int _ENCRST = 31;
        private const int _SAVEPARAMS = 32;
        private const int _CALCPLSOFFSET = 33;
        private const int _SET_LOG_LEVEL = 34;
        private const int _FSCTRL = 35;
        private const int _PTPPLAN = 36;
        private const int _CPPLAN = 37;
        private const int _PTPPLAN_W_SP = 38;
        private const int _CPPLAN_W_SP = 39;
        private const int _SYSCTRL = 40;
        private const int _OPTCPMOVE = 41;
        private const int _OPTCPPLAN = 42;
        private const int _PTPMOVE_MT = 43;
        private const int _MARK_MT = 44;
        private const int _J2R_MT = 45;
        private const int _R2J_MT = 46;
        private const int _PTPPLAN_MT = 47;
        private const int _PTPPLAN_W_SP_MT = 48;
        private const int _JNTRMOVE = 49;
        private const int _JNTRMOVE_WO_CHK = 50;
        private const int _CPRMOVE = 51;
        private const int _CPRPLAN = 52;
        private const int _CPRPLAN_W_SP = 53;
        private const int _SUSPENDM = 54;
        private const int _RESUMEM = 55;
        private const int _GETMT = 56;
        private const int _RELBRK = 57;
        private const int _CLPBRK = 58;
        private const int _ARCMOVE = 59;
        private const int _CIRMOVE = 60;
        private const int _MMARK = 61;

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

        private object[] ChkRes(int cmdId, string buf)
        {
            if (string.IsNullOrEmpty(buf))
            {
                Console.WriteLine("chkRes buffer empty");
                return new object[] { false, 99, 1 };
            }

            var jsonObj = JsonSerializer.Deserialize<JsonElement>(buf);
            int cmd = jsonObj.GetProperty("cmd").GetInt32();
            if (cmdId != cmd)
            {
                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string retryBuf = Encoding.ASCII.GetString(recvBuf, 0, len);
                jsonObj = JsonSerializer.Deserialize<JsonElement>(retryBuf);
                if (cmdId != jsonObj.GetProperty("cmd").GetInt32())
                {
                    Console.WriteLine("chkRes cmd ID error");
                    return new object[] { false, 99, 1 };
                }
            }

            var results = jsonObj.GetProperty("results");
            if (results.ValueKind == JsonValueKind.Array)
            {
                var arr = new object[results.GetArrayLength()];
                int i = 0;
                foreach (var item in results.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                    {
                        if (item.TryGetDouble(out double doubleVal))
                            arr[i++] = doubleVal;
                        else if (item.TryGetInt32(out int intVal))
                            arr[i++] = intVal;
                    }
                    else if (item.ValueKind == JsonValueKind.True)
                        arr[i++] = true;
                    else if (item.ValueKind == JsonValueKind.False)
                        arr[i++] = false;
                    else
                        arr[i++] = item.ToString();
                }
                return arr;
            }
            else
            {
                return new object[] { results.ToString() };
            }
        }

        // NOP Command
        public object[] Nop()
        {
            var paramObj = new
            {
                cmd = _NOP,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            Console.WriteLine(jsonStr);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_NOP, buf);
            }
        }

        // Servo Control
        public object[] SvCtrl(int sw)
        {
            var paramObj = new
            {
                cmd = _SVSW,
                @params = new object[] { sw }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SVSW, buf);
            }
        }

        // Pulse Move
        public object[] PlsMove(int ax1, int ax2, int ax3, int ax4, int ax5, int ax6, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _PLSMOVE,
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PLSMOVE, buf);
            }
        }

        // Motor Move
        public object[] MtrMove(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, double speed, double acct, double dacct)
        {
            double[] axes = new double[]
            {
                ax1 * Math.PI / 180.0,
                ax2 * Math.PI / 180.0,
                ax3 * Math.PI / 180.0,
                ax4 * Math.PI / 180.0,
                ax5 * Math.PI / 180.0,
                ax6 * Math.PI / 180.0
            };

            var paramObj = new
            {
                cmd = _MTRMOVE,
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_MTRMOVE, buf);
            }
        }

        // Joint Move
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
                cmd = _JNTMOVE,
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_JNTMOVE, buf);
            }
        }

        // PTP Move
        public object[] PtpMove(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _PTPMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        x / 1000.0,
                        y / 1000.0,
                        z / 1000.0,
                        rz * Math.PI / 180.0,
                        ry * Math.PI / 180.0,
                        rx * Math.PI / 180.0,
                        posture,
                        rbcoord
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPMOVE, buf);
            }
        }

        // CP Move
        public object[] CpMove(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        x / 1000.0,
                        y / 1000.0,
                        z / 1000.0,
                        rz * Math.PI / 180.0,
                        ry * Math.PI / 180.0,
                        rx * Math.PI / 180.0,
                        posture,
                        rbcoord
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPMOVE, buf);
            }
        }

        // Set Tool
        public object[] SetTool(int tid, double offx, double offy, double offz, double offrz, double offry, double offrx)
        {
            var paramObj = new
            {
                cmd = _SET_TOOL,
                @params = new object[]
                {
                    tid,
                    offx / 1000.0,
                    offy / 1000.0,
                    offz / 1000.0,
                    offrz * Math.PI / 180.0,
                    offry * Math.PI / 180.0,
                    offrx * Math.PI / 180.0
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SET_TOOL, buf);
            }
        }

        // Change Tool
        public object[] ChangeTool(int tid)
        {
            var paramObj = new
            {
                cmd = _CHANGE_TOOL,
                @params = new object[] { tid }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CHANGE_TOOL, buf);
            }
        }

        // Mark - Get current robot position
        public object[] Mark()
        {
            var paramObj = new
            {
                cmd = _MARK,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_MARK, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    // Convert from meters to mm
                    retList[1] = Convert.ToDouble(retList[1]) * 1000.0;
                    retList[2] = Convert.ToDouble(retList[2]) * 1000.0;
                    retList[3] = Convert.ToDouble(retList[3]) * 1000.0;
                    // Convert from radians to degrees
                    retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                    retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                    retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                }
                return retList;
            }
        }

        // JMark - Get current joint position
        public object[] JMark()
        {
            var paramObj = new
            {
                cmd = _JMARK,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_JMARK, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    if (!_linearJointSupport)
                    {
                        // Convert from radians to degrees
                        retList[1] = Convert.ToDouble(retList[1]) * 180.0 / Math.PI;
                        retList[2] = Convert.ToDouble(retList[2]) * 180.0 / Math.PI;
                        retList[3] = Convert.ToDouble(retList[3]) * 180.0 / Math.PI;
                        retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                        retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                        retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    }
                }
                return retList;
            }
        }

        // Version
        public object[] Version()
        {
            var paramObj = new
            {
                cmd = _VERSION,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_VERSION, buf);
            }
        }

        // Acquire Permission
        public object[] AcqPermission()
        {
            var paramObj = new
            {
                cmd = _ACQ_PERMISSION,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ACQ_PERMISSION, buf);
            }
        }

        // Release Permission
        public object[] RelPermission()
        {
            var paramObj = new
            {
                cmd = _REL_PERMISSION,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_REL_PERMISSION, buf);
            }
        }

        // R2J - Robot coordinates to Joint coordinates conversion
        public object[] R2J(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord)
        {
            var paramObj = new
            {
                cmd = _R2J,
                @params = new object[]
                {
                    x / 1000.0,
                    y / 1000.0,
                    z / 1000.0,
                    rz * Math.PI / 180.0,
                    ry * Math.PI / 180.0,
                    rx * Math.PI / 180.0,
                    posture,
                    rbcoord
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_R2J, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    if (!_linearJointSupport)
                    {
                        // Convert from radians to degrees
                        retList[1] = Convert.ToDouble(retList[1]) * 180.0 / Math.PI;
                        retList[2] = Convert.ToDouble(retList[2]) * 180.0 / Math.PI;
                        retList[3] = Convert.ToDouble(retList[3]) * 180.0 / Math.PI;
                        retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                        retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                        retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    }
                }

                return retList;
            }
        }


        // J2R - Joint coordinates to Robot coordinates conversion
        public object[] J2R(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, int rbcoord)
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
                    ax6 * Math.PI / 180.0,
                    rbcoord
                };
            }
            var paramObj = new
            {
                cmd = _J2R,
                @params = axes
            };
            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_J2R, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    // Convert from meters to mm
                    retList[1] = Convert.ToDouble(retList[1]) * 1000.0;
                    retList[2] = Convert.ToDouble(retList[2]) * 1000.0;
                    retList[3] = Convert.ToDouble(retList[3]) * 1000.0;

                    // Convert from radians to degrees
                    retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                    retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                    retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                }

                return retList;
            }
        }

        // Async Motion Control
        public object[] AsyncM(int sw)
        {
            var paramObj = new
            {
                cmd = _ASYNCM,
                @params = new object[] { sw }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ASYNCM, buf);
            }
        }

        // Pass Motion Control
        public object[] PassM(int sw)
        {
            var paramObj = new
            {
                cmd = _PASSM,
                @params = new object[] { sw }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PASSM, buf);
            }
        }


        // Overlap Control
        public object[] Overlap(double distance)
        {
            var paramObj = new
            {
                cmd = _OVERLAP,
                @params = new object[] { distance / 1000.0 } // Convert mm to m
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_OVERLAP, buf);
            }
        }

        // IO Control
        public object[] IoCtrl(int wordno, int dataL, int maskL, int dataH, int maskH)
        {
            var paramObj = new
            {
                cmd = _IOCTRL,
                @params = new object[] {
                    wordno,
                    dataL,
                    maskL,
                    dataH,
                    maskH
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_IOCTRL, buf);
            }
        }

        // Zone Control
        public object[] Zone(int pulse)
        {
            var paramObj = new
            {
                cmd = _ZONE,
                @params = new object[] { pulse }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ZONE, buf);
            }
        }

        // System Status
        public object[] SysSts(int typ)
        {
            var paramObj = new
            {
                cmd = _SYSSTS,
                @params = new object[] { typ }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SYSSTS, buf);
            }
        }

        // Tool Relative Move
        public object[] TrMove(double x, double y, double z, double rz, double ry, double rx, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _TRMOVE,
                @params = new object[]
                {
                   new double[]
                   {
                       x / 1000.0,
                       y / 1000.0,
                       z / 1000.0,
                       rz * Math.PI / 180.0,
                       ry * Math.PI / 180.0,
                       rx * Math.PI / 180.0
                   },
                   speed / 1000.0,
                   acct,
                   dacct
                }

            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_TRMOVE, buf);
            }

        }

        // Abort Motion
        public object[] AbortM()
        {
            var paramObj = new
            {
                cmd = _ABORTM,
                @params = new object[] { }
            };
            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);
                return ChkRes(_ABORTM, buf);
            }
        }

        // Join Motion
        public object[] JoinM()
        {
            var paramObj = new
            {
                cmd = _JOINM,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_JOINM, buf);
            }
        }

        // SL Speed Control
        public object[] SlSpeed(double spd)
        {
            var paramObj = new
            {
                cmd = _SLSPEED,
                @params = new object[] { spd }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SLSPEED, buf);
            }
        }

        // Set MDO
        public object[] SetMdo(int mdoid, int portno, int value, int kind, double distance)
        {
            var paramObj = new
            {
                cmd = _SET_MDO,
                @params = new object[]
                {
                    mdoid,
                    portno,
                    value,
                    kind,
                    distance / 1000.0 // Convert mm to m
                }
            };
            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);
                return ChkRes(_SET_MDO, buf);
            }
        }


        // Enable MDO
        public object[] EnableMdo(int bitfield)
        {
            var paramObj = new
            {
                cmd = _ENABLE_MDO,
                @params = new object[] { bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            Console.WriteLine(jsonStr);

            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ENABLE_MDO, buf);
            }
        }

        // Disable MDO
        public object[] DisableMdo(int bitfield)
        {
            var paramObj = new
            {
                cmd = _DISABLE_MDO,
                @params = new object[] { bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_DISABLE_MDO, buf);
            }
        }

        // PMark - Get pulse position
        public object[] PMark(int sw)
        {
            var paramObj = new
            {
                cmd = _PMARK,
                @params = new object[] { sw }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PMARK, buf);
            }
        }

        // Encoder Reset
        public object[] EncRst(int bitfield)
        {
            var paramObj = new
            {
                cmd = _ENCRST,
                @params = new object[] { bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ENCRST, buf);
            }
        }

        // Save Parameters
        public object[] SaveParams()
        {
            var paramObj = new
            {
                cmd = _SAVEPARAMS,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SAVEPARAMS, buf);
            }
        }

        // Calculate Pulse Offset
        public object[] CalcPlsOffset(int typ, int bitfield)
        {
            var paramObj = new
            {
                cmd = _CALCPLSOFFSET,
                @params = new object[] { typ, bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CALCPLSOFFSET, buf);
            }
        }

        // Set Log Level
        public object[] SetLogLevel(int level)
        {
            var paramObj = new
            {
                cmd = _SET_LOG_LEVEL,
                @params = new object[] { level }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SET_LOG_LEVEL, buf);
            }
        }

        // Force Sensor Control
        public object[] FsCtrl(int cmd)
        {
            var paramObj = new
            {
                cmd = _FSCTRL,
                @params = new object[] { cmd }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_FSCTRL, buf);
            }
        }

        // OptCpMove - Optimized CP Move
        public object[] OptCpMove(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _OPTCPMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        x / 1000.0,
                        y / 1000.0,
                        z / 1000.0,
                        rz * Math.PI / 180.0,
                        ry * Math.PI / 180.0,
                        rx * Math.PI / 180.0,
                        posture,
                        rbcoord
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_OPTCPMOVE, buf);
            }
        }

        // PtpPlan - PTP Motion Plan
        public object[] PtpPlan(double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _PTPPLAN,
                @params = new object[]
                {
                    new double[]
                    {
                        ex / 1000.0,
                        ey / 1000.0,
                        ez / 1000.0,
                        erz * Math.PI / 180.0,
                        ery * Math.PI / 180.0,
                        erx * Math.PI / 180.0,
                        eposture,
                        erbcoord
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPPLAN, buf);
            }
        }

        // CpPlan - CP Motion Plan
        public object[] CpPlan(double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPPLAN,
                @params = new object[]
                {
                    new double[]
                    {
                        ex / 1000.0,
                        ey / 1000.0,
                        ez / 1000.0,
                        erz * Math.PI / 180.0,
                        ery * Math.PI / 180.0,
                        erx * Math.PI / 180.0,
                        eposture,
                        erbcoord
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPPLAN, buf);
            }
        }

        // PtpPlan with Start Point
        public object[] PtpPlanWSp(double sx, double sy, double sz, double srz, double sry, double srx, int sposture, int srbcoord,
                                   double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord,
                                   double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _PTPPLAN_W_SP,
                @params = new object[]
                {
                    new double[]
                    {
                        sx / 1000.0, sy / 1000.0, sz / 1000.0,
                        srz * Math.PI / 180.0, sry * Math.PI / 180.0, srx * Math.PI / 180.0,
                        sposture, srbcoord
                    },
                    new double[]
                    {
                        ex / 1000.0, ey / 1000.0, ez / 1000.0,
                        erz * Math.PI / 180.0, ery * Math.PI / 180.0, erx * Math.PI / 180.0,
                        eposture, erbcoord
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPPLAN_W_SP, buf);
            }
        }

        // CpPlan with Start Point
        public object[] CpPlanWSp(double sx, double sy, double sz, double srz, double sry, double srx, int sposture, int srbcoord,
                                  double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord,
                                  double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPPLAN_W_SP,
                @params = new object[]
                {
                    new double[]
                    {
                        sx / 1000.0, sy / 1000.0, sz / 1000.0,
                        srz * Math.PI / 180.0, sry * Math.PI / 180.0, srx * Math.PI / 180.0,
                        sposture, srbcoord
                    },
                    new double[]
                    {
                        ex / 1000.0, ey / 1000.0, ez / 1000.0,
                        erz * Math.PI / 180.0, ery * Math.PI / 180.0, erx * Math.PI / 180.0,
                        eposture, erbcoord
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPPLAN_W_SP, buf);
            }
        }

        // OptCpPlan - Optimized CP Plan
        public object[] OptCpPlan(double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _OPTCPPLAN,
                @params = new object[]
                {
                    new double[]
                    {
                        ex / 1000.0,
                        ey / 1000.0,
                        ez / 1000.0,
                        erz * Math.PI / 180.0,
                        ery * Math.PI / 180.0,
                        erx * Math.PI / 180.0,
                        eposture,
                        erbcoord
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_OPTCPPLAN, buf);
            }
        }

        // SysCtrl - System Control
        public object[] SysCtrl(int ctrlid, int arg)
        {
            var paramObj = new
            {
                cmd = _SYSCTRL,
                @params = new object[] { ctrlid, arg }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SYSCTRL, buf);
            }
        }

        // PtpMove Multi-Turn
        public object[] PtpMoveMT(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord, int multiturn, int ikSolverOption, double speed, double acct, double dacct)
        {
            if ((multiturn & 0xFF000000) == 0xFF000000)
            {
                ikSolverOption = 0x00000000;
            }

            var paramObj = new
            {
                cmd = _PTPMOVE_MT,
                @params = new object[]
                {
                    new object[]
                    {
                        x / 1000.0,
                        y / 1000.0,
                        z / 1000.0,
                        rz * Math.PI / 180.0,
                        ry * Math.PI / 180.0,
                        rx * Math.PI / 180.0,
                        posture,
                        rbcoord,
                        multiturn,
                        ikSolverOption
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPMOVE_MT, buf);
            }
        }

        // Mark Multi-Turn
        public object[] MarkMT()
        {
            var paramObj = new
            {
                cmd = _MARK_MT,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_MARK_MT, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    // Convert from meters to mm
                    retList[1] = Convert.ToDouble(retList[1]) * 1000.0;
                    retList[2] = Convert.ToDouble(retList[2]) * 1000.0;
                    retList[3] = Convert.ToDouble(retList[3]) * 1000.0;
                    // Convert from radians to degrees
                    retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                    retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                    retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    retList[9] = Convert.ToInt32(retList[9]);
                }
                return retList;
            }
        }

        // J2R Multi-Turn
        public object[] J2RMT(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, int rbcoord)
        {
            object[] axes;
            if (_linearJointSupport)
            {
                axes = new object[] { ax1, ax2, ax3, ax4, ax5, ax6, rbcoord };
            }
            else
            {
                axes = new object[]
                {
                    ax1 * Math.PI / 180.0,
                    ax2 * Math.PI / 180.0,
                    ax3 * Math.PI / 180.0,
                    ax4 * Math.PI / 180.0,
                    ax5 * Math.PI / 180.0,
                    ax6 * Math.PI / 180.0,
                    rbcoord
                };
            }

            var paramObj = new
            {
                cmd = _J2R_MT,
                @params = axes
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_J2R_MT, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    // Convert from meters to mm
                    retList[1] = Convert.ToDouble(retList[1]) * 1000.0;
                    retList[2] = Convert.ToDouble(retList[2]) * 1000.0;
                    retList[3] = Convert.ToDouble(retList[3]) * 1000.0;
                    // Convert from radians to degrees
                    retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                    retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                    retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    retList[9] = Convert.ToInt32(retList[9]);
                }
                return retList;
            }
        }

        // R2J Multi-Turn
        public object[] R2JMT(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord, int multiturn, int ikSolverOption)
        {
            if ((multiturn & 0xFF000000) == 0xFF000000)
            {
                ikSolverOption = 0x00000000;
            }

            var paramObj = new
            {
                cmd = _R2J_MT,
                @params = new object[]
                {
                    x / 1000.0,
                    y / 1000.0,
                    z / 1000.0,
                    rz * Math.PI / 180.0,
                    ry * Math.PI / 180.0,
                    rx * Math.PI / 180.0,
                    posture,
                    rbcoord,
                    multiturn,
                    ikSolverOption
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_R2J_MT, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    if (!_linearJointSupport)
                    {
                        // Convert from radians to degrees
                        retList[1] = Convert.ToDouble(retList[1]) * 180.0 / Math.PI;
                        retList[2] = Convert.ToDouble(retList[2]) * 180.0 / Math.PI;
                        retList[3] = Convert.ToDouble(retList[3]) * 180.0 / Math.PI;
                        retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                        retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                        retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    }
                }
                return retList;
            }
        }

        // PtpPlan Multi-Turn
        public object[] PtpPlanMT(double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord, int emultiturn, int ikSolverOption, double speed, double acct, double dacct)
        {
            if ((emultiturn & 0xFF000000) == 0xFF000000)
            {
                ikSolverOption = 0x00000000;
            }

            var paramObj = new
            {
                cmd = _PTPPLAN_MT,
                @params = new object[]
                {
                    new object[]
                    {
                        ex / 1000.0,
                        ey / 1000.0,
                        ez / 1000.0,
                        erz * Math.PI / 180.0,
                        ery * Math.PI / 180.0,
                        erx * Math.PI / 180.0,
                        eposture,
                        erbcoord,
                        emultiturn,
                        ikSolverOption
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPPLAN_MT, buf);
            }
        }

        // PtpPlan with Start Point Multi-Turn
        public object[] PtpPlanWSpMT(double sx, double sy, double sz, double srz, double sry, double srx, int sposture, int srbcoord, int smultiturn, int sikSolverOption,
                                     double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord, int emultiturn, int eikSolverOption,
                                     double speed, double acct, double dacct)
        {
            if ((smultiturn & 0xFF000000) == 0xFF000000)
            {
                sikSolverOption = 0x00000000;
            }
            if ((emultiturn & 0xFF000000) == 0xFF000000)
            {
                eikSolverOption = 0x00000000;
            }

            var paramObj = new
            {
                cmd = _PTPPLAN_W_SP_MT,
                @params = new object[]
                {
                    new object[]
                    {
                        sx / 1000.0, sy / 1000.0, sz / 1000.0,
                        srz * Math.PI / 180.0, sry * Math.PI / 180.0, srx * Math.PI / 180.0,
                        sposture, srbcoord, smultiturn, sikSolverOption
                    },
                    new object[]
                    {
                        ex / 1000.0, ey / 1000.0, ez / 1000.0,
                        erz * Math.PI / 180.0, ery * Math.PI / 180.0, erx * Math.PI / 180.0,
                        eposture, erbcoord, emultiturn, eikSolverOption
                    },
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_PTPPLAN_W_SP_MT, buf);
            }
        }

        // Joint Relative Move (with soft limit check)
        public object[] JntrMove(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, double speed, double acct, double dacct)
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
                cmd = _JNTRMOVE,
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                // Handle empty buffer retry logic like Python
                if (string.IsNullOrEmpty(buf))
                {
                    Console.WriteLine("buffer is empty!!!!!");
                    while (string.IsNullOrEmpty(buf))
                    {
                        sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                        _stream.Write(sendBuf, 0, sendBuf.Length);

                        recvBuf = new byte[_N_RCVBUF];
                        len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                        buf = Encoding.ASCII.GetString(recvBuf, 0, len);
                    }
                }

                return ChkRes(_JNTRMOVE, buf);
            }
        }

        // Joint Relative Move (without soft limit check)
        public object[] JntrMoveWoChk(double ax1, double ax2, double ax3, double ax4, double ax5, double ax6, double speed, double acct, double dacct)
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
                cmd = _JNTRMOVE_WO_CHK,
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

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_JNTRMOVE_WO_CHK, buf);
            }
        }

        // CP Relative Move - Global coordinate system relative motion
        public object[] CprMove(double x, double y, double z, double rz, double ry, double rx, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPRMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        x / 1000.0,
                        y / 1000.0,
                        z / 1000.0,
                        rz * Math.PI / 180.0,
                        ry * Math.PI / 180.0,
                        rx * Math.PI / 180.0
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPRMOVE, buf);
            }
        }

        // CP Relative Plan
        public object[] CprPlan(double dx, double dy, double dz, double drz, double dry, double drx, double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPRPLAN,
                @params = new object[]
                {
                    new double[]
                    {
                        dx / 1000.0,
                        dy / 1000.0,
                        dz / 1000.0,
                        drz * Math.PI / 180.0,
                        dry * Math.PI / 180.0,
                        drx * Math.PI / 180.0
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPRPLAN, buf);
            }
        }

        // CP Relative Plan with Start Point
        public object[] CprPlanWSp(double x, double y, double z, double rz, double ry, double rx, int posture, int rbcoord,
                                   double dx, double dy, double dz, double drz, double dry, double drx,
                                   double speed, double acct, double dacct)
        {
            var paramObj = new
            {
                cmd = _CPRPLAN_W_SP,
                @params = new object[]
                {
                    new object[]
                    {
                        x / 1000.0, y / 1000.0, z / 1000.0,
                        rz * Math.PI / 180.0, ry * Math.PI / 180.0, rx * Math.PI / 180.0,
                        posture, rbcoord
                    },
                    new double[]
                    {
                        dx / 1000.0, dy / 1000.0, dz / 1000.0,
                        drz * Math.PI / 180.0, dry * Math.PI / 180.0, drx * Math.PI / 180.0
                    },
                    speed / 1000.0,
                    acct,
                    dacct
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CPRPLAN_W_SP, buf);
            }
        }

        // Suspend Motion
        public object[] SuspendM(double tmout)
        {
            var paramObj = new
            {
                cmd = _SUSPENDM,
                @params = new object[] { tmout }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_SUSPENDM, buf);
            }
        }

        // Resume Motion
        public object[] ResumeM()
        {
            var paramObj = new
            {
                cmd = _RESUMEM,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_RESUMEM, buf);
            }
        }

        // Get Multi-Turn value
        public object[] GetMt(double bx, double by, double bz, double brz, double bry, double brx, int posture, int rbcoord, int multiturn,
                              string strX, string strY, string strZ, string strRz, string strRy, string strRx)
        {
            var paramObj = new
            {
                cmd = _GETMT,
                @params = new object[]
                {
                    new object[]
                    {
                        bx / 1000.0, by / 1000.0, bz / 1000.0,
                        brz * Math.PI / 180.0, bry * Math.PI / 180.0, brx * Math.PI / 180.0,
                        posture, rbcoord, multiturn
                    },
                    new string[] { strX, strY, strZ, strRz, strRy, strRx }
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_GETMT, buf);
            }
        }

        // Release Brake
        public object[] RelBrk(int bitfield)
        {
            var paramObj = new
            {
                cmd = _RELBRK,
                @params = new object[] { bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_RELBRK, buf);
            }
        }

        // Clamp Brake
        public object[] ClpBrk(int bitfield)
        {
            var paramObj = new
            {
                cmd = _CLPBRK,
                @params = new object[] { bitfield }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CLPBRK, buf);
            }
        }

        // Arc Move
        public object[] ArcMove(double px, double py, double pz, double prz, double pry, double prx, int pposture, int prbcoord,
                                double ex, double ey, double ez, double erz, double ery, double erx, int eposture, int erbcoord,
                                double speed, double acct, double dacct, int orientation)
        {
            var paramObj = new
            {
                cmd = _ARCMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        px / 1000.0, py / 1000.0, pz / 1000.0,
                        prz * Math.PI / 180.0, pry * Math.PI / 180.0, prx * Math.PI / 180.0,
                        pposture, prbcoord
                    },
                    new double[]
                    {
                        ex / 1000.0, ey / 1000.0, ez / 1000.0,
                        erz * Math.PI / 180.0, ery * Math.PI / 180.0, erx * Math.PI / 180.0,
                        eposture, erbcoord
                    },
                    speed / 1000.0,
                    acct,
                    dacct,
                    orientation
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_ARCMOVE, buf);
            }
        }

        // Circle Move
        public object[] CirMove(double p1x, double p1y, double p1z, double p1rz, double p1ry, double p1rx, int p1posture, int p1rbcoord,
                                double p2x, double p2y, double p2z, double p2rz, double p2ry, double p2rx, int p2posture, int p2rbcoord,
                                double speed, double acct, double dacct, int orientation)
        {
            var paramObj = new
            {
                cmd = _CIRMOVE,
                @params = new object[]
                {
                    new double[]
                    {
                        p1x / 1000.0, p1y / 1000.0, p1z / 1000.0,
                        p1rz * Math.PI / 180.0, p1ry * Math.PI / 180.0, p1rx * Math.PI / 180.0,
                        p1posture, p1rbcoord
                    },
                    new double[]
                    {
                        p2x / 1000.0, p2y / 1000.0, p2z / 1000.0,
                        p2rz * Math.PI / 180.0, p2ry * Math.PI / 180.0, p2rx * Math.PI / 180.0,
                        p2posture, p2rbcoord
                    },
                    speed / 1000.0,
                    acct,
                    dacct,
                    orientation
                }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                return ChkRes(_CIRMOVE, buf);
            }
        }

        // MMark - Get current motor position
        public object[] MMark()
        {
            var paramObj = new
            {
                cmd = _MMARK,
                @params = new object[] { }
            };

            string jsonStr = JsonSerializer.Serialize(paramObj);
            lock (_lock)
            {
                byte[] sendBuf = Encoding.ASCII.GetBytes(jsonStr);
                _stream.Write(sendBuf, 0, sendBuf.Length);

                byte[] recvBuf = new byte[_N_RCVBUF];
                int len = _stream.Read(recvBuf, 0, _N_RCVBUF);
                string buf = Encoding.ASCII.GetString(recvBuf, 0, len);

                var retList = ChkRes(_MMARK, buf);
                if (retList.Length > 0 && retList[0].ToString() == "True")
                {
                    if (!_linearJointSupport)
                    {
                        // Convert from radians to degrees
                        retList[1] = Convert.ToDouble(retList[1]) * 180.0 / Math.PI;
                        retList[2] = Convert.ToDouble(retList[2]) * 180.0 / Math.PI;
                        retList[3] = Convert.ToDouble(retList[3]) * 180.0 / Math.PI;
                        retList[4] = Convert.ToDouble(retList[4]) * 180.0 / Math.PI;
                        retList[5] = Convert.ToDouble(retList[5]) * 180.0 / Math.PI;
                        retList[6] = Convert.ToDouble(retList[6]) * 180.0 / Math.PI;
                    }
                }
                return retList;
            }
        }

    }
}
