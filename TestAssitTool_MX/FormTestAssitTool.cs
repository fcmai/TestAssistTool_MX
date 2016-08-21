using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using 数据采集系统.通用类;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TestAssitTool
{
    public partial class FrmTestAssitTool : Form
    {
        public FrmTestAssitTool()
        {
            InitializeComponent();
        }
        #region 自动反馈控制变量
        //用于暂停某个串口通道数据反馈
        private bool bPauseCom_1 = false;
        private bool bPauseCom_2 = false;
        private bool bPauseCom_3 = false;

        //用于反馈某个串口通道错误数据 
        private bool bAnswerFalseCom_1 = false;
        private bool bAnswerFalseCom_2 = false;
        private bool bAnswerFalseCom_3 = false;
        //       private bool bCom1 = false;
        //        private bool bCom2 = false;
        //       private bool bCom3 = false;
        #endregion 自动反馈控制变量

        #region dll声明
        [DllImport("MethodsDLL.dll")]
        extern static int NumOfAisle(char[] Msg);
        [DllImport("MethodsDLL.dll")]
        extern static int Add(int a, int b);
        [DllImport("MethodsDLL.dll")]
        extern static void GetTemperature_Positive(int mode, char[] Msg, ref byte temperature);
        [DllImport("MethodsDLL.dll")]
        extern static void GetTemperature_Negative(int mode, char[] Msg, ref byte temperature);
        [DllImport("MethodsDLL.dll")]
        extern static int crc16_modbus(char[] crc, int length);
        [DllImport("MethodsDLL.dll")]
        extern static int xstrtoshortint(char[] str);
        #endregion


        ActUtlTypeLib.ActUtlType actCpu = new ActUtlTypeLib.ActUtlType();//动态新建 MX Component控件
        private SerialPort comm = new SerialPort();
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private long received_count = 0;//接收计数
        private long send_count = 0;//发送计数

        private SerialPort comm2 = new SerialPort();
        private StringBuilder builder2 = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private long received_count2 = 0;//接收计数
        private long send_count2 = 0;//发送计数


        #region  PLC以太网 通信
        delegate void dele_actOpen();        //dele_actOpen d_actOpen = actOpen;
        private void actOpen()
        {
            string path = "\\Ini\\SystemConfig.ini";
            string strSec = "SystemConfig";
            IniFile iniFile = new IniFile(path);
            int iReturnCode = 0;				//Return code
            //int iLogicalStationNumber = 1;		//LogicalStationNumber for ActUtlType

            actCpu.ActLogicalStationNumber = iniFile.IniReadInt(strSec, "站号", 1);
            actCpu.ActLogicalStationNumber = 1;

            iReturnCode = actCpu.Open();
            iReturnCode += 1;
        }

        delegate void dele_actClose();
        private void actClose()
        {
            int backnum = actCpu.Close();
            backnum += 1;
        }

        delegate void dele_actRead(string address, out string b);//AxActUtlTypeLib.AxActUtlType sender          // dele_actRead d_actRead= actRead ;
        private static long lastErrTick;
        private void actRead(string address, out string str_out)
        {
            int iReturnCode;				//Return code
            String szDeviceName = address; const int iNumberOfData = 1;			//Data for 'iNumberOfData'
            short[] arrDeviceValue;		    //Data for 'DeviceValue'

            // int iNumber;					//Loop counter
            //System.String[] arrData;	    //Array for 'Data'
            arrDeviceValue = new short[iNumberOfData];

            iReturnCode = actCpu.ReadDeviceBlock2(szDeviceName, iNumberOfData, out arrDeviceValue[0]);

            str_out = (arrDeviceValue[0]).ToString();
            if (iReturnCode != 0)
            {
                if (System.Environment.TickCount - lastErrTick > 3000)
                {
                    lastErrTick = System.Environment.TickCount;

                }
            }
            iReturnCode += 1;
        }

        delegate void dele_actWrite(string address, string b);
        private void actWrite(string address, string b)
        {
            int iReturnCode;				//Return code
            String szDeviceName = address;
            const int iNumberOfData = 1;			//Data for 'iNumberOfData'
            short[] arrDeviceValue;		    //Data for 'DeviceValue'
            // int iNumber;					//Loop counter
            //System.String[] arrData;	    //Array for 'Data'
            arrDeviceValue = new short[iNumberOfData];
            arrDeviceValue[0] = Convert.ToInt16(b);
            iReturnCode = actCpu.WriteDeviceBlock2(szDeviceName, iNumberOfData, ref arrDeviceValue[0]);
        }

        #endregion PLC tonxin

        int numOfAdress = 0;//记录需要动态生成的个数
        string label_adress = "00";//保存ini标签name
        int numOfGroup;//记录分了多少组
        string strOriginal = string.Empty;
        ArrayList lstOriginal = new ArrayList();//保存修改前的值
        ArrayList lstAddress = new ArrayList();//保存需要修改的地址
        static string path = "\\Ini\\SystemConfig.ini";
        string strSec = "SystemConfig";
        IniFile iniFile = new IniFile(path);
        string checkboxname = string.Empty;
        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();   //实例化Timer类，设置间隔时间为3000毫秒；
        private void Form1_Load(object sender, EventArgs e)
        {
            actOpen();
            
            #region 动态生成控件-注释掉了
            /*
            lstOriginal.Clear();
            lstAddress.Clear();
            int index = 0;
            for (; label_adress != ""; numOfAdress++)
            {
                string key = string.Format("地址{0}", numOfAdress + 1);
                label_adress = iniFile.IniReadValue(strSec, key);
                actRead(label_adress, out strOriginal);

                lstOriginal.Add(strOriginal);

                lstAddress.Add(label_adress);
                if (label_adress != "" && numOfAdress < 10)
                {
                    CheckBox cb = new CheckBox();
                    cb.Location = new Point(10, 85 + 40 * numOfAdress);
                    cb.Name = "cb" + numOfAdress.ToString();
                    cb.Size = new Size(20, 20);
                    cb.Checked = true;
                    //this.Controls.Add(cb);

                    Label lbl = new Label();//声明一个label
                    lbl.Location = new System.Drawing.Point(30, 90 + 40 * numOfAdress);//设置位置
                    lbl.Size = new Size(40, 40);//设置大小
                    lbl.Text = label_adress;//设置Text值
                    lbl.ForeColor = Color.Black;
                    //this.Controls.Add(lbl);//在当前窗体上添加这个label控件

                    //this.pnlAddrLst.Controls.Add(lbl);//在当前窗体上添加这个label控件

                    Label lbl2 = new Label();//声明一个label
                    lbl2.Location = new System.Drawing.Point(75, 90 + 40 * numOfAdress);//设置位置
                    lbl2.Size = new Size(20, 20);//设置大小
                    lbl2.Name = lstAddress[index].ToString() + "当前值";
                    lbl2.Text = lstOriginal[index].ToString();//设置Text值
                    lbl2.ForeColor = Color.Black;
                    index++;
                    //this.Controls.Add(lbl2);//在当前窗体上添加这个label控件

                    TextBox txtbox = new TextBox();
                    txtbox.Location = new Point(110, 85 + 40 * numOfAdress);
                    txtbox.Size = new Size(40, 30);
                    txtbox.Name = "txtValue" + numOfAdress.ToString();
                    //Controls.Add(txtbox);
                    this.pnlAddrLst.Controls.Add(cb);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl2);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(txtbox);//在当前窗体上添加这个label控件
                }
                else if (label_adress != "" && (numOfAdress >= 10 && numOfAdress < 20))
                {
                    CheckBox cb = new CheckBox();
                    cb.Location = new Point(180, 85 + 40 * (numOfAdress - 10));
                    cb.Name = "cb" + numOfAdress.ToString();
                    cb.Size = new Size(20, 20);
                    cb.Checked = true;
                    //this.Controls.Add(cb);

                    Label lbl = new Label();//声明一个label
                    lbl.Location = new System.Drawing.Point(200, 90 + 40 * (numOfAdress - 10));//设置位置
                    lbl.Size = new Size(40, 40);//设置大小
                    lbl.Text = label_adress;//设置Text值
                    lbl.ForeColor = Color.Black;
                    // this.Controls.Add(lbl);//在当前窗体上添加这个label控件

                    Label lbl2 = new Label();//声明一个label
                    lbl2.Location = new System.Drawing.Point(245, 90 + 40 * (numOfAdress - 10));//设置位置
                    lbl2.Size = new Size(20, 20);//设置大小
                    lbl2.Name = lstAddress[index].ToString() + "当前值";
                    lbl2.Text = lstOriginal[index].ToString();//设置Text值
                    index++;
                    lbl2.ForeColor = Color.Black;
                    //this.Controls.Add(lbl2);//在当前窗体上添加这个label控件

                    TextBox txtbox = new TextBox();
                    txtbox.Location = new Point(280, 85 + 40 * (numOfAdress - 10));
                    txtbox.Size = new Size(40, 30);
                    txtbox.Name = "txtValue" + numOfAdress.ToString();
                    // Controls.Add(txtbox);
                    this.pnlAddrLst.Controls.Add(cb);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl2);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(txtbox);//在当前窗体上添加这个label控件
                }
                else if (label_adress != "" && numOfAdress >= 20)
                {
                    CheckBox cb = new CheckBox();
                    cb.Location = new Point(360, 85 + 40 * (numOfAdress - 20));
                    cb.Name = "cb" + numOfAdress.ToString();
                    cb.Size = new Size(20, 20);
                    cb.Checked = true;
                    //this.Controls.Add(cb);

                    Label lbl = new Label();//声明一个label
                    lbl.Location = new System.Drawing.Point(380, 90 + 40 * (numOfAdress - 20));//设置位置
                    lbl.Size = new Size(40, 40);//设置大小
                    lbl.Text = label_adress;//设置Text值
                    lbl.ForeColor = Color.Black;
                    //this.Controls.Add(lbl);//在当前窗体上添加这个label控件

                    Label lbl2 = new Label();//声明一个label
                    lbl2.Location = new System.Drawing.Point(420, 90 + 40 * (numOfAdress - 12));//设置位置
                    lbl2.Size = new Size(20, 20);//设置大小
                    lbl2.Name = lstAddress[index].ToString() + "当前值";
                    lbl2.Text = lstOriginal[index].ToString();//设置Text值
                    index++;
                    lbl2.ForeColor = Color.Black;
                    //this.Controls.Add(lbl2);//在当前窗体上添加这个label控件

                    TextBox txtbox = new TextBox();
                    txtbox.Location = new Point(460, 85 + 40 * (numOfAdress - 20));
                    txtbox.Size = new Size(40, 30);
                    txtbox.Name = "txtValue" + numOfAdress.ToString();
                    //Controls.Add(txtbox);
                    this.pnlAddrLst.Controls.Add(cb);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(lbl2);//在当前窗体上添加这个label控件
                    this.pnlAddrLst.Controls.Add(txtbox);//在当前窗体上添加这个label控件
                }
            }
            numOfGroup = (numOfAdress - 1) / 10 + 1;
            for (int i = 0; i < numOfGroup; i++)
            {
                Label lbl = new Label();
                lbl.Location = new Point(65 + 170 * i, 60);
                lbl.Text = "当前值";
                lbl.Size = new Size(45, 35);
                lbl.ForeColor = Color.Black;
                //Controls.Add(lbl);

                Label lbl2 = new Label();
                lbl2.Location = new Point(115 + 170 * i, 60);
                lbl2.Text = "设定值";
                lbl2.Size = new Size(50, 40);
                lbl2.ForeColor = Color.Black;
                //Controls.Add(lbl2);
                this.pnlAddrLst.Controls.Add(lbl); //在当前窗体上添加这个label控件
                this.pnlAddrLst.Controls.Add(lbl2); //在当前窗体上添加这个label控件
            }
            */
            #endregion 动态初始化控件

            buttonOpenClose.BackColor = Color.Red;
            buttonOpenClose2.BackColor = Color.Red;
            //初始化下拉串口名称列表框
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            comboPortName.Items.AddRange(ports);
            comboPortName.SelectedIndex = comboPortName.Items.Count > 0 ? 0 : -1;
            comboBaudrate.SelectedIndex = comboBaudrate.Items.IndexOf("9600");
            cbxDelimiter.SelectedIndex = cbxDelimiter.Items.IndexOf("0D0A");
            //初始化SerialPort对象
            comm.NewLine = "\r\n";
            comm.RtsEnable = true;//根据实际情况吧。

            //添加事件注册
            comm.DataReceived += comm_DataReceived;

            //初始化下拉串口名称列表框
            string[] ports2 = SerialPort.GetPortNames();
            Array.Sort(ports2);
            comboPortName2.Items.AddRange(ports);
            comboPortName2.SelectedIndex = comboPortName.Items.Count > 0 ? 0 : -1;
            comboBaudrate2.SelectedIndex = comboBaudrate.Items.IndexOf("9600");
            cbxDelimiter2.SelectedIndex = cbxDelimiter.Items.IndexOf("0D0A");
            //初始化SerialPort对象
            comm2.NewLine = "\r\n";
            comm2.RtsEnable = true;//根据实际情况吧。

            //添加事件注册
            comm2.DataReceived += comm_DataReceived2;

            actClose();
        }
        private void chkbox_CheckedChanged(object sender, EventArgs e)
        {
            string num = checkboxname.Substring(2);
            foreach (Control c in this.pnlAddrLst.Controls)
                if (c is TextBox && (c as TextBox).Name == "txtValue" + num)
                {
                    (c as TextBox).Text = "";
                }
        }
        int clickTimes = 0;//记录点击次数
        private void btnCancelAllChecked_Click(object sender, EventArgs e)
        {

            /*
            if (clickTimes == 0)
            {
                foreach (Control c1 in this.pnlAddrLst.Controls)
                    if (c1 is CheckBox)
                    {
                        (c1 as CheckBox).Checked = false;
                    }
                clickTimes = 1;
                btnCancelAllChecked.Text = "全选";
            }
            else
            {
                foreach (Control c1 in this.pnlAddrLst.Controls)
                    if (c1 is CheckBox)
                    {
                        (c1 as CheckBox).Checked = true;
                    }
                clickTimes = 0;
                btnCancelAllChecked.Text = "取消全选";
            }
            */

        }
        int numOfChecked;//记录checkbox选中的个数
        int numOfContent;//记录textbox里面有内容的个数
        int numOfMatch;//记录选中且对应的txtbox里面有数据的个数
        private void btnSet1_Click(object sender, EventArgs e)
        {
            /*
            numOfChecked = 0;
            for (int i = 0; i < numOfAdress - 1; i++)
            {
                foreach (Control c in this.pnlAddrLst.Controls)
                    if (c is TextBox && c.Name.Equals("txtValue" + i.ToString()))
                    {
                        foreach (Control c1 in this.pnlAddrLst.Controls)
                            if (c1 is CheckBox && c1.Name.Equals("cb" + i.ToString()) && (c1 as CheckBox).Checked == true)
                            {
                                (c as TextBox).Text = "1";
                                numOfChecked++;
                            }
                    }
            }
            */
        }

        private void btnSet0_Click(object sender, EventArgs e)
        {
            //numOfChecked = 0;
            /*
            for (int i = 0; i < numOfAdress - 1; i++)
            {
                foreach (Control c in this.pnlAddrLst.Controls)
                    if (c is TextBox && c.Name.Equals("txtValue" + i.ToString()))
                    {
                        foreach (Control c1 in this.pnlAddrLst.Controls)
                            if (c1 is CheckBox && c1.Name.Equals("cb" + i.ToString()) && (c1 as CheckBox).Checked == true)
                            {
                                (c as TextBox).Text = "0";
                                //numOfChecked++;
                            }
                    }
            }
            */
        }
        ArrayList list = new ArrayList();//获取txtbox里面的值

        private void btnSet_Click(object sender, EventArgs e)
        {
            /*
            list.Clear();
            numOfContent = 0;
            numOfChecked = 0;
            numOfMatch = 0;
            dele_actOpen d_actOpen = actOpen;
            dele_actClose d_actClose = actClose;
            dele_actWrite d_actWrite = actWrite;
            dele_actRead d_actRead = actRead;
            string key = string.Empty;
            string num2 = string.Empty;
            int ii = 0;
            actOpen();
 
            for (int i = 0; i < numOfAdress - 1; i++)
            {
                foreach (Control c in this.pnlAddrLst.Controls)
                    if (c is TextBox && c.Name.Equals("txtValue" + i.ToString()) && (c as TextBox).Text != "")
                    {
                        foreach (Control c1 in this.pnlAddrLst.Controls)
                            if (c1 is CheckBox && c1.Name.Equals("cb" + i.ToString()) && (c1 as CheckBox).Checked == true)
                            {
                                list.Add((c as TextBox).Text);
                                num2 = (c1 as CheckBox).Name.Substring(2);
                                key = string.Format("地址{0}", Convert.ToInt32(num2) + 1);
                                label_adress = iniFile.IniReadValue(strSec, key);
                                actWrite(label_adress, list[ii].ToString());
                                ii++;
                            }
                    }
            }

                 
            for (int j = 0; j < lstAddress.Count; j++)
            {
                foreach (Control c in this.pnlAddrLst.Controls)
                {

                    if (c is Label && (c as Label).Name.Equals(lstAddress[j].ToString() + "当前值"))
                    {
                        actRead(lstAddress[j].ToString(), out strOriginal);
                        (c as Label).Text = strOriginal;
                    }

                }

            }
            Label lblPrompt = new Label();
            lblPrompt.Location = new Point(5, 515);
            lblPrompt.Size = new Size(200, 50);
            lblPrompt.ForeColor = Color.Red;
            lblPrompt.Font = new Font("SimSun", 25);
            lblPrompt.Text = "设置成功！";
            this.pnlAddrLst.Controls.Add(lblPrompt);
            actClose();
            //  }


            t.Interval = 5000;
            t.Tick += new EventHandler(timer1_Tick); //到达时间的时候执行事件；   
            t.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；   
            t.Start();
            // }
            //             else
            //             {
            //                 MessageBoxEx.Show("请正确勾选选择框或者将发送数据输入完整！", 3000);
            //             }
            */
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            foreach (Control c in this.pnlAddrLst.Controls)
            {
                if (c is Label && (c as Label).Text == "设置成功！")
                    (c as Label).Visible = false;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        #region SerialPort1
        void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = comm.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
            byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
            received_count += n;//增加接收计数
            comm.Read(buf, 0, n);//读取缓冲数据
            builder.Clear();//清除字符串构造器的内容
            bool flag = true;
            List<string> lstbuf1 = new List<string>();//填充到这个临时列表中
            //因为要访问ui资源，所以需要使用invoke方式同步ui。
            this.Invoke((EventHandler)(delegate
            {
                //判断是否是显示为16禁止
                if (checkBoxHexView.Checked)
                {
                    //依次的拼接出16进制字符串
                    foreach (byte b in buf)
                    {
                        builder.Append(b.ToString("X2") + " ");
                        //builder.Append(Convert .ToString (b ,16)+ " ");
                    }
                    string timeNow = System.DateTime.Now.ToString();
                    this.txtReceive1.AppendText("[" + timeNow + "]" + builder.ToString() + "\r\n");
                }
                else
                {
                    string strTemp = "";
                    byte[] b = new byte[buf.Length];
                    for (int i = 0; i < buf.Length; i++)
                    {
                        strTemp = buf[i].ToString();
                        b[i] = Convert.ToByte(strTemp, 10);
                    }
                    //按照指定编码将字节数组变为字符串
                    builder.Append(System.Text.Encoding.UTF8.GetString(b));
                    //直接按ASCII规则转换成字符串
                    // builder.Append(Encoding.ASCII.GetString(buf));
                    string timeNow = System.DateTime.Now.ToString();
                    if (builder.ToString().Contains("\r"))
                        this.txtReceive1.AppendText("[" + timeNow + "]" + builder.ToString());
                    else
                        this.txtReceive1.AppendText("[" + timeNow + "]" + builder.ToString() + "\r\n");

                }
                //追加的形式添加到文本框末端，并滚动到最后。

                //修改接收计数
                labelGetCount.Text = "Get:" + received_count.ToString();
            }));
        }
        void comm_DataReceived2(object sender, SerialDataReceivedEventArgs e)
        {
            int n = comm2.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致
            byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据
            received_count2 += n;//增加接收计数
            comm2.Read(buf, 0, n);//读取缓冲数据
            builder2.Clear();//清除字符串构造器的内容
            List<byte> lstbuf2 = new List<byte>();//填充到这个临时列表中
            //因为要访问ui资源，所以需要使用invoke方式同步ui。
            this.Invoke((EventHandler)(delegate
            {
                //判断是否是显示为16禁止
                if (checkBoxHexView2.Checked)
                {
                    //依次的拼接出16进制字符串
                    foreach (byte b in buf)
                    {
                        builder2.Append(b.ToString("X2") + " ");
                    }
                    string timeNow = System.DateTime.Now.ToString();
                    this.txtReceive2.AppendText("[" + timeNow + "]" + builder2.ToString() + "\r\n");
                }
                else
                {
                    string strTemp = "";
                    byte[] b = new byte[buf.Length];
                    for (int i = 0; i < buf.Length; i++)
                    {
                        strTemp = buf[i].ToString();
                        b[i] = Convert.ToByte(strTemp, 10);
                    }
                    //按照指定编码将字节数组变为字符串
                    builder2.Append(System.Text.Encoding.UTF8.GetString(b));
                    string timeNow = System.DateTime.Now.ToString();
                    if (builder2.ToString().Contains("\r"))
                        this.txtReceive2.AppendText("[" + timeNow + "]" + builder2.ToString());
                    else
                        this.txtReceive2.AppendText("[" + timeNow + "]" + builder2.ToString() + "\r\n");
                }
                //追加的形式添加到文本框末端，并滚动到最后。

                //修改接收计数
                labelGetCount2.Text = "Get:" + received_count2.ToString();
            }));
        }
        private void buttonOpenClose_Click(object sender, EventArgs e)
        {
            //根据当前串口对象，来判断操作
            if (comm.IsOpen)
            {
                //打开时点击，则关闭串口
                comm.Close();
            }
            else
            {
                //关闭时点击，则设置好端口，波特率后打开
                comm.PortName = comboPortName.Text;
                comm.BaudRate = int.Parse(comboBaudrate.Text);
                try
                {
                    comm.Open();
                    splitCon_sp.Refresh();
                }
                catch (Exception ex)
                {
                    //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                    comm.Close();
                    //comm = new SerialPort();
                    //现实异常信息给客户。
                    MessageBox.Show(ex.Message);
                }
            }
            //设置按钮的状态
            buttonOpenClose.Text = comm.IsOpen ? "Close" : "Open";
            buttonOpenClose.BackColor = comm.IsOpen ? (Color.Green ) : (Color.Red  );
            buttonSend.Enabled = comm.IsOpen;
        }

        private void checkBoxNewlineGet_CheckedChanged(object sender, EventArgs e)
        {
            txtReceive1.WordWrap = checkBoxAddDelimiter.Checked;
        }
#region 获取分隔符-区分hex和ASCII
        private string GetSplit4Com1()
        {
            string split = string.Empty;
            if (checkBoxHexSend.Checked)
            {
                if (cbxDelimiter.Text == "0D0A")
                    split = "0D 0A";
                else if (cbxDelimiter.Text == "0D")
                    split = "0D";
                else
                {
                    split = "";
                }

                //split = ""; //忽略前面的
            }
            else
            {
                if (cbxDelimiter.Text == "0D0A")
                    split = "\r\n";
                else if (cbxDelimiter.Text == "0D")
                    split = "\r";
                else
                {
                    split = "";
                }
            } return split;
        }

        private string GetSplit4Com2()
        {
            string split = string.Empty;
            if (checkBoxHexSend2.Checked)
            {
                if (cbxDelimiter2.Text == "0D0A")
                    split = "0D 0A";
                else if (cbxDelimiter2.Text == "0D")
                    split = "0D";
                else
                {
                    split = "";
                }

                //split = ""; //忽略前面的
            }
            else
            {
                if (cbxDelimiter2.Text == "0D0A")
                    split = "\r\n";
                else if (cbxDelimiter2.Text == "0D")
                    split = "\r";
                else
                {
                    split = "";
                }
            } return split;
        }

#endregion  获取分隔符-区分hex和ASCII
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (!comm.IsOpen) return;
            string split = string.Empty;
            if (checkBoxAddDelimiter.Checked)
            {
                split = GetSplit4Com1( );

                txtSend1.Text += split;
            }

            //定义一个变量，记录发送了几个字节
            int n = 0;

            //16进制发送
            if (checkBoxHexSend.Checked)
            {
                //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数

                //                 MatchCollection mc = Regex.Matches(                        txtSend1.Text , @"(?i)[\da-f]{2}");
                //                 List<byte> buf = new List<byte>();//填充到这个临时列表中
                //                 //依次添加到列表中
                //                 foreach (Match m in mc)
                //                 {
                //                     buf.Add(byte.Parse(m.Value));
                //                 }
                //                 //转换列表为数组后发送
                //                 comm.Write(buf.ToArray(), 0, buf.Count);
                //                 //记录发送的字节数
                //                 n = buf.Count;
                string aaa = string.Empty;
                if (checkBoxAddDelimiter.Checked)
                    aaa = HexStringToString(txtSend1.Text, System.Text.Encoding.UTF8);
                else
                    aaa = HexStringToString3(txtSend1.Text, System.Text.Encoding.UTF8);
                comm.Write(aaa);
                n = aaa.Length - 1;
                if (split != "")
                    txtSend1.Text = txtSend1.Text.Replace(split, "");

            }
            else//ascii编码直接发送
            {
                //包含换行符

                comm.Write(txtSend1.Text);
                n = txtSend1.Text.Length + 2;
                if (split != "")
                    txtSend1.Text = txtSend1.Text.Replace(split, "");
            }
            send_count += n;//累加发送字节数
            labelSendCount.Text = "Send:" + (send_count).ToString();//更新界面
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            //复位接受和发送的字节数计数器并更新界面。
            send_count = received_count = 0;
            labelGetCount.Text = "Get:0";
            labelSendCount.Text = "Send:0";
        }

        private void btnClear_Click(object sender, EventArgs e)
        {

            /*
            for (int i = 0; i < numOfAdress - 1; i++)
            {
                foreach (Control c in this.pnlAddrLst.Controls)
                    if (c is TextBox && c.Name.Equals("txtValue" + i.ToString()))
                    {
                        foreach (Control c1 in this.pnlAddrLst.Controls)//this.Controls
                            if (c1 is CheckBox && c1.Name.Equals("cb" + i.ToString()) && (c1 as CheckBox).Checked == true)
                            {
                                (c as TextBox).Text = "";
                            }
                    }
            }
            */

        }
        #endregion

        #region SerialPort2
        private void buttonOpenClose2_Click(object sender, EventArgs e)
        {
            //根据当前串口对象，来判断操作
            if (comm2.IsOpen)
            {
                //打开时点击，则关闭串口
                comm2.Close();
            }
            else
            {
                //关闭时点击，则设置好端口，波特率后打开
                comm2.PortName = comboPortName2.Text;
                comm2.BaudRate = int.Parse(comboBaudrate2.Text);
                try
                {
                    comm2.Open();
                }
                catch (Exception ex)
                {
                    //捕获到异常信息，创建一个新的comm对象，之前的不能用了。
                    comm2.Close();
                    //comm2 = new SerialPort();
                    //现实异常信息给客户。
                    MessageBox.Show(ex.Message);
                }
            }
            //设置按钮的状态
            buttonOpenClose2.Text = comm2.IsOpen ? "Close" : "Open";
            buttonOpenClose2.BackColor = comm2.IsOpen ? (Color.Green) : (Color.Red);
            buttonSend2.Enabled = comm2.IsOpen;
        }

        private void buttonReset2_Click(object sender, EventArgs e)
        {
            //复位接受和发送的字节数计数器并更新界面。
            send_count2 = received_count2 = 0;
            labelGetCount2.Text = "Get:0";
            labelSendCount2.Text = "Send:0";
        }

        private void buttonSend2_Click(object sender, EventArgs e)
        {
            //定义一个变量，记录发送了几个字节
            if (!comm2.IsOpen) return;
            string split = string.Empty;
 
            if (checkBoxAddDelimiter2.Checked)
            {
                split = GetSplit4Com2();

                txtSend2.Text += split;
            }

     
            int n = 0;
            //16进制发送
            if (checkBoxHexSend2.Checked)
            {
                //我们不管规则了。如果写错了一些，我们允许的，只用正则得到有效的十六进制数
                //                 MatchCollection mc = Regex.Matches( txtSend2.Text , @"(?i)[\da-f]{2}");
                //                 List<byte> buf = new List<byte>();//填充到这个临时列表中
                //                 //依次添加到列表中
                //                 foreach (Match m in mc)
                //                 {
                //                     buf.Add(byte.Parse(m.Value));
                //                 }
                //              //   buf.Add(byte.Parse("1"));
                //                 //转换列表为数组后发送
                //                 comm2.Write(buf.ToArray(), 0, buf.Count);
                //                 //记录发送的字节数
                //                 n = buf.Count;
                string aaa = string.Empty;
                if (checkBoxAddDelimiter2.Checked)
                    aaa = HexStringToString(txtSend2.Text, System.Text.Encoding.UTF8);
                else
                    aaa = HexStringToString3(txtSend2.Text, System.Text.Encoding.UTF8);
                comm2.Write(aaa);
                n = aaa.Length - 1;
                if (split != "")
                    txtSend2.Text = txtSend2.Text.Replace(split, "");

            }
            else//ascii编码直接发送
            {
                //包含换行符
                comm2.Write(txtSend2.Text);
                n = txtSend2.Text.Length + 2;
                if (split != "")
                    txtSend2.Text = txtSend2.Text.Replace(split, "");
            }
            send_count2 += n;//累加发送字节数
            labelSendCount2.Text = "Send:" + (send_count2).ToString();//更新界面
        }
        #endregion

        //         private void btnAddSplit1_Click(object sender, EventArgs e)
        //         {
        //             string split = string.Empty;
        //             if (checkBoxHexSend.Checked)
        //             {
        //                 if (cbxDelimiter.Text == "0D0A")
        //                     split = "0D 0A";
        //                 else if (cbxDelimiter.Text == "0D")
        //                     split = "0D";
        //                 else
        //                 {
        //                     split = "";
        //                 }
        // 
        //                 split = ""; //忽略前面的
        //             }
        //             else
        //             {
        //                 if (cbxDelimiter.Text == "0D0A")
        //                     split = "\r\n";
        //                 else if (cbxDelimiter.Text == "0D")
        //                     split = "\r";
        //                 else
        //                 {
        //                     split = "";
        //                 }
        //             }
        // 
        // 
        //             txtSend1.Text += split;
        //             txtSend1.Focus();
        //             //让文本框获取焦点 
        //             this.txtSend1.Focus();
        //             //设置光标的位置到文本尾 
        //             this.txtSend1.Select(this.txtSend1.TextLength, 0);
        //             //滚动到控件光标处 
        //             this.txtSend1.ScrollToCaret();
        // 
        // 
        //         }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void toolStripMenuItem_Clear_Click(object sender, EventArgs e)
        {
            txtReceive1.Text = "";
        }

        private void toolStripMenuItem_save_Click(object sender, EventArgs e)
        {
            //System.IO.File.WriteAllLines("C:\\1.txt", new string[] { textBox1.Text, textBox2.Text, comboBox1.Text, comboBox2.Text });
            string data1 = this.txtReceive1.Text;
            //string data2 = this.textBox2.Text;
            //string data3 = this.textBox3.Text;

            StreamWriter sw = new StreamWriter("receive1" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒") + ".txt");//写入到bin目录下的test.txt文件中
            //sw.WriteLine(data1 + ";" + data2 + ";" + data3);//以;隔开
            //
            sw.WriteLine(data1);
            sw.Close();//关闭流，才能写入txt

        }

        private void toolStripMenuItem1_clear_Click(object sender, EventArgs e)
        {
            txtReceive2.Text = "";
        }

        private void toolStripMenuItem2_save_Click(object sender, EventArgs e)
        {
            //System.IO.File.WriteAllLines("C:\\1.txt", new string[] { textBox1.Text, textBox2.Text, comboBox1.Text, comboBox2.Text });
            string data2 = this.txtReceive2.Text;
            //string data2 = this.textBox2.Text;
            //string data3 = this.textBox3.Text;

            StreamWriter sw = new StreamWriter("receive2" + DateTime.Now.ToString("yyyy年MM月dd HH时mm分ss秒") + ".txt");//写入到bin目录下的test.txt文件中
            //sw.WriteLine(data1 + ";" + data2 + ";" + data3);//以;隔开
            //
            sw.WriteLine(data2);
            sw.Close();//关闭流，才能写入txt


        }

        private void gp_ser2_set_Enter(object sender, EventArgs e)
        {

        }

        //         private void btnAddSplit2_Click(object sender, EventArgs e)
        //         {
        //             string split = string.Empty;
        //             if (checkBoxHexSend2.Checked)
        //             {
        //                 if (cbxDelimiter2.Text == "0D0A")
        //                     split = "0D 0A";
        //                 else if (cbxDelimiter2.Text == "0D")
        //                     split = "0D";
        //                 else
        //                 {
        //                     split = "";
        //                 }
        // 
        //                 split = ""; //忽略前面的
        //             }
        //             else
        //             {
        //                 if (cbxDelimiter2.Text == "0D0A")
        //                     split = "\r\n";
        //                 else if (cbxDelimiter2.Text == "0D")
        //                     split = "\r";
        //                 else
        //                 {
        //                     split = "";
        //                 }
        //             }
        //             txtSend2.Text += split;
        //             txtSend2.Focus();
        //             //让文本框获取焦点 
        //             this.txtSend2.Focus();
        //             //设置光标的位置到文本尾 
        //             this.txtSend2.Select(this.txtSend2.TextLength, 0);
        //             //滚动到控件光标处 
        //             this.txtSend2.ScrollToCaret();
        //         }

        private void checkBoxHexView2_CheckedChanged(object sender, EventArgs e)
        {

        }
        #region 10进制和16进制相互装换
        private string StringToHexString(string s, Encoding encode)
        {
            string result = string.Empty;
            byte[] b = encode.GetBytes(s);//按照指定编码将string编程字节数组 
            for (int i = 0; i < b.Length; i++)//逐字节变为16进制字符，以%隔开 {
            {
                result += Convert.ToString(b[i], 16) + " ";
            }

            return result;
        }
        private string HexStringToString(string hs, Encoding encode)
        {
            string strTemp = "";
            byte[] b = new byte[hs.Length / 3 + 1];
            for (int i = 0; i < hs.Length / 3 + 1; i++)
            {
                strTemp = hs.Substring(i * 3, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }
        //这个给16进制和10进制切换时使用
        private string HexStringToString3(string hs, Encoding encode)
        {
            string strTemp = "";
            byte[] b = new byte[hs.Length / 3];
            for (int i = 0; i < hs.Length / 3; i++)
            {
                strTemp = hs.Substring(i * 3, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }
        #endregion
        int clicktimes = 0;
        private void checkBoxHexSend2_CheckedChanged(object sender, EventArgs e)
        {
            // string text=txtSend2 .Text ;
            if (clicktimes == 0)
            {
                string str16 = StringToHexString(txtSend2.Text, System.Text.Encoding.UTF8);
                txtSend2.Text = "";
                txtSend2.Text = str16.Substring(0, str16.Length);
                clicktimes = 1;
            }
            else
            {
                string str10 = HexStringToString3(txtSend2.Text, System.Text.Encoding.UTF8);
                txtSend2.Text = "";
                txtSend2.Text = str10;
                clicktimes = 0;
            }

        }

        private void checkBoxHexSend_CheckedChanged(object sender, EventArgs e)
        {
            if (clicktimes == 0)
            {
                string str16 = StringToHexString(txtSend1.Text, System.Text.Encoding.UTF8);
                txtSend1.Text = "";
                txtSend1.Text = str16.Substring(0, str16.Length);
                clicktimes = 1;
            }
            else
            {
                string str10 = HexStringToString3(txtSend1.Text, System.Text.Encoding.UTF8);
                txtSend1.Text = "";
                txtSend1.Text = str10;
                clicktimes = 0;
            }
        }

        private void pnlAddrLst_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitCon_sp_Paint(object sender, PaintEventArgs e)
        {


        }

        private void gb_ser1_setting_Paint(object sender, PaintEventArgs e)
        {
            //             Graphics g1 = gb_ser1_setting.CreateGraphics();
            //             var Rec1 = new Rectangle(new Point(150, 45), new Size(20, 20));
            //             g1.FillEllipse(Brushes.Red, Rec1);
        }

        private void btnInverseSel_Click(object sender, EventArgs e)
        {
            /*
            foreach (Control c1 in this.pnlAddrLst.Controls)
                if (c1 is CheckBox)
                {
                    (c1 as CheckBox).Checked = !(c1 as CheckBox).Checked;
                }
            */
        }

        private void btnAddSplit2_Click(object sender, EventArgs e)
        {
            string split = GetSplit4Com2();
            txtSend2.Text += split;
            txtSend2.Focus();
            //让文本框获取焦点 
            this.txtSend2.Focus();
            //设置光标的位置到文本尾 
            this.txtSend2.Select(this.txtSend2.TextLength, 0);
            //滚动到控件光标处 
            this.txtSend2.ScrollToCaret();
        }

        private void btnAddSplit1_Click(object sender, EventArgs e)
        {
            string split = GetSplit4Com1();
            txtSend1.Text += split;
            txtSend1.Focus();
            //让文本框获取焦点 
            this.txtSend1.Focus();
            //设置光标的位置到文本尾 
            this.txtSend1.Select(this.txtSend1.TextLength, 0);
            //滚动到控件光标处 
            this.txtSend1.ScrollToCaret();
        }
    }

}

