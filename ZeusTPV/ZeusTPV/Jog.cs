using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ZeusTPV
{

    public enum MOVE_MODE
    {
        IDLE = 0,
        JNTMOVE = 1,
        CPMOVE = 2,
        JOG = 3,
        MOVETO = 4
    }

    public enum MOVE_DIRECTION
    {
        FWD = 0,
        BACK = 1
    }

    public enum DIAL_MODE
    {
        ALIGN = 0,
        LNR = 1,
        PTP = 2
    }

    public class Jog
    {
        // Constants
        private const double JOG_JNT_SPD = 4.0;      // Maximum Joint Speed for Jog Motion
        private const double JOG_CP_SPD = 25.0;     // Maximum Linear(X,Y,Z) Speed for Jog Motion
        private const double JOG_CP_SPD_ROT = 0.07; // Maximum Linear(RZ,RY,RX) Speed for Jog Motion
        private const double MOVE_CP_SPD = 40.0; // Maximum Linear(X,Y,Z) Speed for MoveTo Motion

        // Robot connection
        private Robot _rb;

        // Speed and motion settings
        private double _jogJntSpd;
        private double _jogCpSpd;
        private double _jogCpSpdRot;
        private double _moveCpSpd;
        private double _dialValue;
        private double _dialDeg;
        private double _dialLinSpd;
        private double _dialPtpSpd;
        private double _pitchJnt;
        private double _pitchJntSpd;
        private double _pitchCp;
        private double _pitchCpSpd;
        private double _cpSpd;
        private double _cpSpdRot;
        private double _jntSpd;

        // Position and state tracking
        private object[] _curPos;
        private object[] _curJnt;
        private double[] _jogPos;
        private double[] _jogJnt;
        private double[] _targetPos;
        private double[] _targetJnt;
        private double[] _diffPos;
        private double[] _diffJnt;

        // Motion control
        private MOVE_MODE _jogMode;
        private MOVE_DIRECTION _directionMode;
        private MOVE_MODE _mode;
        private MOVE_MODE _lastMode;
        private bool _isPitchMode;
        private bool _isStop;
        private int _jogCnt;
        private int _cnt;

        // Position history
        private List<double[]> _beforePosList;
        private List<double[]> _beforeJntList;
        private int _cntBeforePos;
        private int _cntBeforeJnt;

        // Threading
        private CancellationTokenSource _cancellationTokenSource;
        private Task _jogThread;
        private Task _dialThread;

        // Events for position updates (optional, for external monitoring)
        public event Action<object[]> PositionUpdated;
        public event Action<object[]> JointUpdated;

        public Jog(string ipAddress = "192.168.0.23", int port = 12345)
        {
            // Initialize robot connection
            _rb = new Robot(ipAddress, port);

            // Initialize speed settings
            _jogJntSpd = JOG_JNT_SPD * 0.5;
            _jogCpSpd = JOG_CP_SPD * 0.5;
            _jogCpSpdRot = JOG_CP_SPD_ROT * 0.5;
            _moveCpSpd = MOVE_CP_SPD * 0.4;

            // Initialize motion settings
            _dialValue = 0;
            _dialDeg = 0.1;
            _dialLinSpd = 0.1;
            _dialPtpSpd = 0.1;
            _pitchJnt = 0.1;
            _pitchJntSpd = 1;
            _pitchCp = 0.1;
            _pitchCpSpd = 10;
            _cpSpd = 10;
            _cpSpdRot = JOG_CP_SPD_ROT * 0.5;
            _jntSpd = 1.6;

            // Initialize arrays
            _jogPos = new double[6];
            _jogJnt = new double[6];
            _targetPos = new double[6];
            _targetJnt = new double[6];
            _diffPos = new double[6];
            _diffJnt = new double[6];

            // Initialize state
            _jogMode = MOVE_MODE.IDLE;
            _directionMode = MOVE_DIRECTION.FWD;
            _mode = MOVE_MODE.CPMOVE;
            _lastMode = MOVE_MODE.CPMOVE;
            _isPitchMode = true;
            _isStop = true;
            _jogCnt = 0;
            _cnt = 0;

            // Initialize position history
            _beforePosList = new List<double[]>();
            _beforeJntList = new List<double[]>();
            _cntBeforePos = 0;
            _cntBeforeJnt = 0;

            // Initialize cancellation token
            _cancellationTokenSource = new CancellationTokenSource();


            Initialize();

        }

        private void InitialMotionSetting()
        {
            _dialValue = 0;
            _dialDeg = 0.1;
            _dialLinSpd = 0.1;
            _dialPtpSpd = 0.1;
            _pitchJnt = 0.1;
            _pitchJntSpd = 1;
            _pitchCp = 0.1;
            _pitchCpSpd = 10;
            _cpSpd = 10;
            _cpSpdRot = JOG_CP_SPD_ROT * 0.5;
            _jntSpd = 1.6;
        }

        public bool Initialize()
        {
            try
            {
                // Open robot connection
                if (!_rb.Open())
                {
                    Console.WriteLine("Failed to open robot connection");
                    return false;
                }

                // Acquire permission
                object[] ret;
                do
                {
                    ret = _rb.AcqPermission();
                    if (ret[0] is bool success1 && !success1)
                    {
                        Console.WriteLine("Failed to acquire permission, retrying...");
                        //await Task.Delay(1000);
                    }
                } while (ret[0] is bool success && !success);

                Console.WriteLine("Successfully acquired robot permission");

                // Set async and pass mode (if these methods exist)
                _rb.AsyncM(1);
                _rb.PassM(1);

                // Start worker threads
                StartWorkerThreads();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex.Message}");
                return false;
            }
        }

        private void StartWorkerThreads()
        {
            //Start jog motion thread
            _jogThread = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        RobotMotion();
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Jog thread error: {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);

            // Start dial thread (placeholder for future dial functionality)
            _dialThread = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Dial motion logic would go here
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Dial thread error: {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void GetPosition()
        {
            try
            {
                _curPos = _rb.MarkMT();
                _curJnt = _rb.JMark();

                Console.WriteLine($"Current Position: {string.Join(", ", _curPos)}");
                Console.WriteLine($"Current Joint: {string.Join(", ", _curJnt)}");

                // Trigger events
                PositionUpdated?.Invoke(_curPos);
                JointUpdated?.Invoke(_curJnt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting position: {ex.Message}");
            }
        }

        //TODO Change speed with UI slider control
        public void ChangeJogSpeed(double speedPercent)
        {
            if (_isPitchMode)
            {
                if (_mode == MOVE_MODE.CPMOVE)
                {
                    _pitchCp = 0.01 * Math.Pow(10, speedPercent / 100.0 * 2 + 1); // Range 0.01 to 10
                    Console.WriteLine($"Pitch CP: {_pitchCp}");
                }
                else if (_mode == MOVE_MODE.JNTMOVE)
                {
                    _pitchJnt = 0.001 * Math.Pow(10, speedPercent / 100.0 * 3 + 1); // Range 0.001 to 10
                    Console.WriteLine($"Pitch Joint: {_pitchJnt}");
                }
            }
            else
            {
                _cpSpd = JOG_CP_SPD * speedPercent / 100.0;
                _cpSpdRot = JOG_CP_SPD_ROT * speedPercent / 100.0;
                _moveCpSpd = MOVE_CP_SPD * speedPercent / 100.0;
                _jntSpd = JOG_JNT_SPD * speedPercent / 100.0;

                Console.WriteLine($"Speed changed - CP (isPitchMode false): {_cpSpd}, Joint: {_jntSpd}");
            }
        }

        public void JogReady(double[] offset, MOVE_MODE jogModeParam, bool isRotational = false)
        {
            _lastMode = _mode;
            GetPosition();

            // Take a look of this
            InitialMotionSetting();


            if (isRotational)
            {
                ChangeJogSpeed(50); // Default 50% speed
                _cpSpd = _cpSpdRot;
            }
            else
            {
                ChangeJogSpeed(50);
            }

            if (_isPitchMode)
            {
                ExecutePitchMotion(offset, jogModeParam);
            }
            else
            {
                SetupContinuousJog(offset, jogModeParam);
            }
        }

        private void ExecutePitchMotion(double[] offset, MOVE_MODE jogModeParam)
        {
            if (jogModeParam == MOVE_MODE.JNTMOVE)
            {
                for (int i = 0; i < 6; i++)
                {
                    _jogJnt[i] = offset[i] * _pitchJnt * 10;
                }

                double[] currentJnt = ExtractJointValues(_curJnt);
                _rb.JntMove(
                    currentJnt[0] + Math.Round(_jogJnt[0], 2),
                    currentJnt[1] + Math.Round(_jogJnt[1], 2),
                    currentJnt[2] + Math.Round(_jogJnt[2], 2),
                    currentJnt[3] + Math.Round(_jogJnt[3], 2),
                    currentJnt[4] + Math.Round(_jogJnt[4], 2),
                    currentJnt[5] + Math.Round(_jogJnt[5], 2),
                    _pitchJntSpd, 0.2, 0.2);
            }
            else if (jogModeParam == MOVE_MODE.CPMOVE)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i < 3)
                        _jogPos[i] = offset[i] * _pitchCp;
                    else
                        _jogPos[i] = offset[i] * _pitchCp * 10;
                }

                double[] currentPos = ExtractPositionValues(_curPos);
                _rb.CpMove(
                    Math.Round(currentPos[0] + _jogPos[0], 2),
                    Math.Round(currentPos[1] + _jogPos[1], 2),
                    Math.Round(currentPos[2] + _jogPos[2], 2),
                    Math.Round(currentPos[3] + _jogPos[3], 2),
                    Math.Round(currentPos[4] + _jogPos[4], 2),
                    Math.Round(currentPos[5] + _jogPos[5], 2),
                    (int)currentPos[6], (int)currentPos[7], _cpSpd, 0.2, 0.2);
            }
        }

        private void SetupContinuousJog(double[] offset, MOVE_MODE jogModeParam)
        {
            if (jogModeParam == MOVE_MODE.JNTMOVE)
            {
                Array.Copy(offset, _jogJnt, 6);
            }
            else if (jogModeParam == MOVE_MODE.CPMOVE)
            {
                Array.Copy(offset, _jogPos, 6);
            }

            _jogMode = MOVE_MODE.JOG;
            _mode = jogModeParam;
            _isStop = false;
        }

        private void RobotMotion()
        {
            if (_isStop)
            {
                Thread.Sleep(30);
                return;
            }

            try
            {
                // Check system status - motion buffer size
                object[] sysStatus = _rb.SysSts(5);
                if (sysStatus.Length > 1)
                {
                    int statusValue = Convert.ToInt32(sysStatus[1]);
                    if (statusValue < 4 && statusValue >= 0)
                    {
                        if (_jogMode == MOVE_MODE.JOG)
                        {
                            _jogCnt++;

                            if (_mode == MOVE_MODE.JNTMOVE)
                            {
                                ExecuteJointJog();
                            }
                            else if (_mode == MOVE_MODE.CPMOVE)
                            {
                                ExecuteCartesianJog();
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{sysStatus.Length}, {sysStatus[1]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Robot motion error: {ex.Message}");
            }
        }

        private void ExecuteJointJog()
        {
            double[] currentJnt = ExtractJointValues(_curJnt);
            _rb.JntMove(
                currentJnt[0] + _jogJnt[0] * _jogCnt,
                currentJnt[1] + _jogJnt[1] * _jogCnt,
                currentJnt[2] + _jogJnt[2] * _jogCnt,
                currentJnt[3] + _jogJnt[3] * _jogCnt,
                currentJnt[4] + _jogJnt[4] * _jogCnt,
                currentJnt[5] + _jogJnt[5] * _jogCnt,
                _jntSpd, 0.4, 0.4);
        }

        private void ExecuteCartesianJog()
        {
            double[] currentPos = ExtractPositionValues(_curPos);
            _rb.CpMove(
                currentPos[0] + _jogPos[0] * _jogCnt,
                currentPos[1] + _jogPos[1] * _jogCnt,
                currentPos[2] + _jogPos[2] * _jogCnt,
                currentPos[3] + _jogPos[3] * _jogCnt,
                currentPos[4] + _jogPos[4] * _jogCnt,
                currentPos[5] + _jogPos[5] * _jogCnt,
                (int)currentPos[6], (int)currentPos[7], _cpSpd, 0.4, 0.4);
        }

        // Jog control methods
        public void JogXPositive() => JogReady(new double[] { 1, 0, 0, 0, 0, 0 }, MOVE_MODE.CPMOVE);
        public void JogXNegative() => JogReady(new double[] { -1, 0, 0, 0, 0, 0 }, MOVE_MODE.CPMOVE);
        public void JogYPositive() => JogReady(new double[] { 0, 1, 0, 0, 0, 0 }, MOVE_MODE.CPMOVE);
        public void JogYNegative() => JogReady(new double[] { 0, -1, 0, 0, 0, 0 }, MOVE_MODE.CPMOVE);
        public void JogZPositive() => JogReady(new double[] { 0, 0, 1, 0, 0, 0 }, MOVE_MODE.CPMOVE);
        public void JogZNegative() => JogReady(new double[] { 0, 0, -1, 0, 0, 0 }, MOVE_MODE.CPMOVE);

        public void JogRzPositive() => JogReady(new double[] { 0, 0, 0, 0.1, 0, 0 }, MOVE_MODE.CPMOVE, true);
        public void JogRzNegative() => JogReady(new double[] { 0, 0, 0, -0.1, 0, 0 }, MOVE_MODE.CPMOVE, true);
        public void JogRyPositive() => JogReady(new double[] { 0, 0, 0, 0, 0.1, 0 }, MOVE_MODE.CPMOVE, true);
        public void JogRyNegative() => JogReady(new double[] { 0, 0, 0, 0, -0.1, 0 }, MOVE_MODE.CPMOVE, true);
        public void JogRxPositive() => JogReady(new double[] { 0, 0, 0, 0, 0, 0.1 }, MOVE_MODE.CPMOVE, true);
        public void JogRxNegative() => JogReady(new double[] { 0, 0, 0, 0, 0, -0.1 }, MOVE_MODE.CPMOVE, true);

        public void JogJ1Positive() => JogReady(new double[] { 0.1, 0, 0, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ1Negative() => JogReady(new double[] { -0.1, 0, 0, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ2Positive() => JogReady(new double[] { 0, 0.1, 0, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ2Negative() => JogReady(new double[] { 0, -0.1, 0, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ3Positive() => JogReady(new double[] { 0, 0, 0.1, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ3Negative() => JogReady(new double[] { 0, 0, -0.1, 0, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ4Positive() => JogReady(new double[] { 0, 0, 0, 0.1, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ4Negative() => JogReady(new double[] { 0, 0, 0, -0.1, 0, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ5Positive() => JogReady(new double[] { 0, 0, 0, 0, 0.1, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ5Negative() => JogReady(new double[] { 0, 0, 0, 0, -0.1, 0 }, MOVE_MODE.JNTMOVE);
        public void JogJ6Positive() => JogReady(new double[] { 0, 0, 0, 0, 0, 0.1 }, MOVE_MODE.JNTMOVE);
        public void JogJ6Negative() => JogReady(new double[] { 0, 0, 0, 0, 0, -0.1 }, MOVE_MODE.JNTMOVE);

        public void StopJog()
        {
            Console.WriteLine("Jog stopped");
            JogReady(new double[] { 0, 0, 0, 0, 0, 0 }, MOVE_MODE.IDLE);
            _mode = _lastMode;

            if (!_isPitchMode)
            {
                _rb.AbortM();
                _jogCnt = 0;
                _isStop = true;
            }
        }

        public void Record()
        {
            GetPosition();

            double[] posRecord = ExtractPositionValues(_curPos);
            double[] jntRecord = ExtractJointValues(_curJnt);

            _beforePosList.Add((double[])posRecord.Clone());
            _beforeJntList.Add((double[])jntRecord.Clone());

            _cntBeforePos++;
            _cntBeforeJnt++;

            Console.WriteLine($"Position recorded - Count: {_cntBeforePos}");
        }

        public void HandAlign()
        {
            GetPosition();
            double[] currentPos = ExtractPositionValues(_curPos);

            double alignedRz = AlignValue(currentPos[3]);
            double alignedRy = AlignValue(currentPos[4]);
            double alignedRx = AlignValue(currentPos[5]);

            Console.WriteLine($"Hand Align: {currentPos[0]}, {currentPos[1]}, {currentPos[2]}, {alignedRz}, {alignedRy}, {alignedRx}");

            _rb.CpMove(currentPos[0], currentPos[1], currentPos[2],
                      alignedRz, alignedRy, alignedRx,
                      (int)currentPos[6], -1, _moveCpSpd, 0.4, 0.4);
        }

        private double AlignValue(double val)
        {
            int sign = val >= 0 ? 1 : -1;
            int ival = (int)Math.Ceiling(Math.Abs(val));
            int mod = ival % 90;
            ival = (ival / 90) * 90;
            if (mod > 45)
                ival += 90;
            return sign * ival;
        }

        public void MoveToPosition(double x, double y, double z, double rz, double ry, double rx, int posture, int mt)
        {
            Record();
            _rb.CpMove(x, y, z, rz, ry, rx, posture, -1, _moveCpSpd, 0.4, 0.4);
        }

        public void MoveToJoint(double j1, double j2, double j3, double j4, double j5, double j6)
        {
            Record();
            _rb.JntMove(j1, j2, j3, j4, j5, j6, _jntSpd, 0.4, 0.4);
        }

        public void MoveBack()
        {
            if (_cntBeforePos > 0)
            {
                double[] lastPos = _beforePosList[_cntBeforePos - 1];
                _rb.CpMove(lastPos[0], lastPos[1], lastPos[2], lastPos[3], lastPos[4], lastPos[5],
                          (int)lastPos[6], -1, _moveCpSpd, 0.4, 0.4);
            }
        }

        // Helper methods
        private double[] ExtractPositionValues(object[] posArray)
        {
            if (posArray == null || posArray.Length < 9)
                return new double[9];

            double[] result = new double[9];
            for (int i = 0; i < Math.Min(9, posArray.Length - 1); i++)
            {
                if (posArray[i + 1] != null)
                    result[i] = Convert.ToDouble(posArray[i + 1]);
            }
            return result;
        }

        private double[] ExtractJointValues(object[] jntArray)
        {
            if (jntArray == null || jntArray.Length < 7)
                return new double[6];

            double[] result = new double[6];
            for (int i = 0; i < 6; i++)
            {
                if (jntArray[i + 1] != null)
                    result[i] = Convert.ToDouble(jntArray[i + 1]);
            }
            return result;
        }

        // Properties
        public bool IsPitchMode
        {
            get => _isPitchMode;
            set => _isPitchMode = value;
        }

        public MOVE_MODE CurrentMode
        {
            get => _mode;
            set => _mode = value;
        }

        public bool IsMoving => !_isStop;

        public double[] CurrentPosition => ExtractPositionValues(_curPos);
        public double[] CurrentJoint => ExtractJointValues(_curJnt);

        public void Dispose()
        {
            // Stop all operations
            _isStop = true;
            _cancellationTokenSource?.Cancel();

            //Wait for threads to complete
            try
            {
                Task.WaitAll(new[] { _jogThread, _dialThread }, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for threads: {ex.Message}");
            }

            // Release robot permission and close connection
            try
            {
                _rb?.RelPermission();
                _rb?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing robot: {ex.Message}");
            }

            _cancellationTokenSource?.Dispose();
            Console.WriteLine("Jog system disposed");
        }
    }


}

//// Example usage class
//public class JogExample
//{
//    public static async Task Main(string[] args)
//    {
//        var jog = new Jog("localhost", 12345);

//        // Subscribe to position updates
//        jog.PositionUpdated += (pos) => Console.WriteLine($"Position updated: {string.Join(", ", pos)}");
//        jog.JointUpdated += (jnt) => Console.WriteLine($"Joint updated: {string.Join(", ", jnt)}");

//        if (await jog.Initialize())
//        {
//            Console.WriteLine("Jog system initialized successfully");

//            // Example operations
//            jog.GetPosition();
//            await Task.Delay(1000);

//            // Jog in X direction
//            Console.WriteLine("Jogging X positive...");
//            jog.JogXPositive();
//            await Task.Delay(2000);
//            jog.StopJog();

//            // Hand align
//            Console.WriteLine("Hand aligning...");
//            jog.HandAlign();
//            await Task.Delay(3000);

//            Console.WriteLine("Press any key to exit...");
//            Console.ReadKey();
//        }
//        else
//        {
//            Console.WriteLine("Failed to initialize jog system");
//        }

//        jog.Dispose();
//    }
//}