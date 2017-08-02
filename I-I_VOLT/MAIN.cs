using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.IO.Ports;

using USBCANI;


namespace I_I_VOLT
{       
    public partial class MAIN : Form
    {
        static UInt32 m_devtype = 3,canReciveNum=0;//USBCAN1
        UInt16 ID = 0;
        UInt16 gNewID = 0;

        //static UInt32 m_devtype = 21;//USBCAN-2e-u
        //usb-e-u 波特率
        static UInt32[] GCanBrTab = new UInt32[10]{
	                0x060003, 0x060004, 0x060007,
		                0x1C0008, 0x1C0011, 0x160023,
		                0x1C002C, 0x1600B3, 0x1C00E0,
		                0x1C01C1
                };

        bool CAN_Opend=false;

        public MAIN()
        {
            InitializeComponent();
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            comboBox1.SelectedIndex = 1;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
           
        }

        public void recive()
        {   
           
            ControlCAN.VCI_CAN_OBJ[] CAN_ReceiveData = new ControlCAN.VCI_CAN_OBJ[50];
            UInt32 ReadDataNum=0;
            
            while (true)
            {
                UInt32 DataNum = ControlCAN.VCI_GetReceiveNum(m_devtype, 0, 0);
                ID = Convert.ToUInt16(ID_textBox.Text);
                if (DataNum > 0)
                {
                    ReadDataNum = ControlCAN.VCI_Receive(m_devtype, 0, 0,CAN_ReceiveData,(UInt32)CAN_ReceiveData.Length,10);
                    for (int i = 0; i < ReadDataNum; i++)
                    {
                        canReciveNum++;
                        if (canReciveNum % 10 == 1)
                        {
                            label1.BackColor = Color.Red;
                        }
                        else if (canReciveNum % 5 == 1)
                        {
                            label1.BackColor = Color.Blue;
                        }
 
                        if (showdata_checkBox.Checked)
                        {
                            if (canReciveNum >= 200)
                            {
                                canReciveNum = 0;

                                try
                                {
                                    listBox1.Items.Clear();
                                }
                                catch
                                {
                                    ;
                                }
                            }
                            string remoteFlag, exFlag, dataRecive = "";
                            if (CAN_ReceiveData[i].RemoteFlag == 1) remoteFlag = " 远程帧";
                            else remoteFlag = " 数据帧";

                            if (CAN_ReceiveData[i].ExternFlag == 1) exFlag = " 扩展帧";
                            else exFlag = " 标准帧";
                            for (int j = 0; j < CAN_ReceiveData[i].DataLen; j++)
                            {
                                dataRecive += ("0x" + CAN_ReceiveData[i].Data[j].ToString("X2") + " ");
                            }
                            // textBox1.Text += "ID：0x" + CAN_ReceiveData[i].ID.ToString("X4") + remoteFlag + exFlag + " 数据长度" + CAN_ReceiveData[i].DataLen.ToString() +" 数据: "+dataRecive+ "\r\n";

                            listBox1.Items.Add("ID：0x" + CAN_ReceiveData[i].ID.ToString("X4") + remoteFlag + exFlag + " 数据长度" + CAN_ReceiveData[i].DataLen.ToString() + " 数据: " + dataRecive);
                            this.listBox1.SelectedIndex = this.listBox1.Items.Count-1;
                        }
                        if (CAN_ReceiveData[i].ID == (0x500 | ID * 16))
                        {
                            int curent = 0, voltage = 0;
                            Int64 cap = 0;
                            if ((CAN_ReceiveData[i].Data[0] & 0x80) == 0x80)
                            {
                                curent = (CAN_ReceiveData[i].Data[0] & 0x7f) * 256 + CAN_ReceiveData[i].Data[1] - 16000;
                                current_textBox.Text = ((float)curent / 10).ToString("0.0");
                            }
                            else
                            {
                                current_textBox.Text = "无效";
                            }
                            if ((CAN_ReceiveData[i].Data[2] & 0x80) == 0x80)
                            {
                                cap = ((CAN_ReceiveData[i].Data[2] & 0x7f) * 256 * 256 * 256 + CAN_ReceiveData[i].Data[3] * 256 * 256 + CAN_ReceiveData[i].Data[4] * 256 + CAN_ReceiveData[i].Data[5] - 100000000);
                                cap_textBox.Text = (cap).ToString();
                            }
                            else
                                cap_textBox.Text = "无效";

                            if ((CAN_ReceiveData[i].Data[6] & 0x80) == 0x80)
                            {
                                voltage = (CAN_ReceiveData[i].Data[6] & 0x7f) * 256 + CAN_ReceiveData[i].Data[7];
                                voltage_textBox.Text = ((float)voltage / 10).ToString("0.0");
                            }
                            else
                            {
                                voltage_textBox.Text = "无效";
                            }
                        }
                        else if (CAN_ReceiveData[i].ID == (0x501 | ID * 16))
                        {
                            int inside_temperature = 0, outside_temperature=0;
                            inside_temperature = (CAN_ReceiveData[i].Data[0] & 0x7f) * 256 + CAN_ReceiveData[i].Data[1] - 400;
                            temperature_textBox.Text = ((float)inside_temperature / 10).ToString("0.0");

                             //outside_temperature = (CAN_ReceiveData[i].Data[3] & 0x7f) * 256 + CAN_ReceiveData[i].Data[4];
                             //out_Temp.Text= ((float)outside_temperature / 10).ToString("0.0");
                            outside_temperature = CAN_ReceiveData[i].Data[3] - 40;
                            out_Temp.Text = outside_temperature.ToString("0.0");

                        }
                        else if (CAN_ReceiveData[i].ID == (0x502 | ID * 16))
                        {
                            UInt16 software_version = 0, hardware_version = 0;
                            UInt32 SN = 0;

                            software_version = (UInt16)(CAN_ReceiveData[i].Data[0] * 256 + CAN_ReceiveData[i].Data[1]);
                            sorftware_version_textBox.Text = (software_version / 100.0).ToString("0.00");

                            hardware_version = (UInt16)(CAN_ReceiveData[i].Data[2] * 256 + CAN_ReceiveData[i].Data[3]);
                            hardware_version_textBox.Text = (hardware_version / 100.0).ToString("0.00");

                            SN=(UInt32)(CAN_ReceiveData[i].Data[4]* 256 * 256 * 256 + CAN_ReceiveData[i].Data[5] * 256 * 256 + CAN_ReceiveData[i].Data[6] * 256 + CAN_ReceiveData[i].Data[7]);
                            SN_textBox.Text = SN.ToString().PadLeft(10, '0');
                        }
                        else if (CAN_ReceiveData[i].ID == (0x503 | ID * 16))
                        {
                            voltage1.Text = (((CAN_ReceiveData[i].Data[0] & 0x7f) * 256 + CAN_ReceiveData[i].Data[1])/10.0).ToString("0.0");
                            voltage2.Text = (((CAN_ReceiveData[i].Data[2] & 0x7f) * 256 + CAN_ReceiveData[i].Data[3]) / 10.0).ToString("0.0");
                            voltage3.Text = (((CAN_ReceiveData[i].Data[4] & 0x7f) * 256 + CAN_ReceiveData[i].Data[5]) / 10.0).ToString("0.0");
                            voltage4.Text = (((CAN_ReceiveData[i].Data[6] & 0x7f) * 256 + CAN_ReceiveData[i].Data[7]) / 10.0).ToString("0.0");
                        }
                        else if (CAN_ReceiveData[i].ID == 0x701)
                        {
                            switch (CAN_ReceiveData[i].Data[1]) 
                            {
                                case 0x01:
                                    Int32 b = CAN_ReceiveData[i].Data[2] * 256 * 256 * 256 + CAN_ReceiveData[i].Data[3] * 256 * 256 + CAN_ReceiveData[i].Data[4] * 256  + CAN_ReceiveData[i].Data[5];
                                    MessageBox.Show("电流校准第一步完成：b=" + b.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    button3.Enabled = false;
                                    button4.Enabled = true;
                                    break;
                                case 0x02:
                                    Int32 k = CAN_ReceiveData[i].Data[2] * 256 * 256 * 256 + CAN_ReceiveData[i].Data[3] * 256 * 256 + CAN_ReceiveData[i].Data[4] * 256 + CAN_ReceiveData[i].Data[5];
                                    MessageBox.Show("电流校准第二步完成：k=" + k.ToString(), "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    button4.Enabled = false;
                                    button3.Enabled = true;
                                    break;
                                case 0x03:
                                    break;
                                case 0x06:
                                    if(CAN_ReceiveData[i].Data[2]==0x07)
                                        MessageBox.Show("恢复出厂值成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    else
                                        MessageBox.Show("校准清除完毕！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    break;
                                case 0x10:
                                    if(CAN_ReceiveData[i].Data[0]==gNewID)
                                    {
                                        ID_textBox.Text = gNewID.ToString();
                                        MessageBox.Show("ID修改成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    break;
                                case 0x11:
                                    MessageBox.Show("SN修改成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    break;
                                default: break;
                            }
                        }
                    }

                }
            }
        }

        void messageSend( ControlCAN.VCI_CAN_OBJ CAN_SendData)
        {

        }

        Thread nonParameterThread;
        private void button1_Click(object sender, EventArgs e)
        {
            ControlCAN.VCI_INIT_CONFIG CAN_Init = new ControlCAN.VCI_INIT_CONFIG();
            //Config device
            CAN_Init.AccCode = 0x00000000;
            CAN_Init.AccMask = 0xFFFFFFFF;
            CAN_Init.Filter = 1;
            CAN_Init.Mode = 0;
            if (comboBox3.SelectedIndex == 0)
            {
                CAN_Init.Timing0 = 0x01;
                CAN_Init.Timing1 = 0x1c;
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                CAN_Init.Timing0 = 0x00;
                CAN_Init.Timing1 = 0x1c;
            }
            else if (comboBox3.SelectedIndex == 2)
            {
                CAN_Init.Timing0 = 0x00;
                CAN_Init.Timing1 = 0x14;
            }
            
            if (button1.Text == "打开USBCAN-I设备")
            {
                if (ControlCAN.VCI_OpenDevice(3, 0, 0) != 1)
                {
                    MessageBox.Show("打开设备失败,请检查设备是否连接或已开启！", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    if (ControlCAN.VCI_InitCAN(3, 0, 0, ref CAN_Init) != 1)
                    {
                        MessageBox.Show("初始化CAN失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        ControlCAN.VCI_CloseDevice(3, 0);
                        return;
                    }
                    else
                    {
                        ControlCAN.VCI_StartCAN(3, 0, 0);
                        button1.Text = "关闭USBCAN-I设备";

                        nonParameterThread = new Thread(new ThreadStart(recive));
                        nonParameterThread.IsBackground = true;
                        nonParameterThread.Start();
                        CAN_Opend = true;
                        button2.Enabled = true;
                        button3.Enabled = true;
                        button5.Enabled = true;
                        button6.Enabled = true;
                        button7.Enabled = true;
                        button8.Enabled = true;
                        button9.Enabled = true;
                    }

                }
            }
            else
            {
                ControlCAN.VCI_CloseDevice(3, 0);
                button1.Text = "打开USBCAN-I设备";

                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                button8.Enabled = false;
                button9.Enabled = false;
                try
                {
                    nonParameterThread.Abort();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    MessageBox.Show("线程关闭失败!", "警告", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void MAIN_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CAN_Opend)
            {
                ControlCAN.VCI_CloseDevice(3, 0);
                button1.Text = "打开USBCAN-I设备";
                nonParameterThread.Abort();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            CAN_SendData[0].DataLen = 8;
            CAN_SendData[0].Data = new Byte[8];

            if(alldevice_checkBox.Checked)
                CAN_SendData[0].Data[0] = (byte)0xff;
            else
                CAN_SendData[0].Data[0] = (byte)ID;
   
            CAN_SendData[0].Data[1] = 0x14;
            CAN_SendData[0].Data[2] = 0;
            CAN_SendData[0].Data[3] = 0;
            CAN_SendData[0].Data[4] = 0;
            CAN_SendData[0].Data[5] = 0;
            CAN_SendData[0].Data[6] = 0;
            CAN_SendData[0].Data[7] = 0;

            CAN_SendData[0].ExternFlag = 0;
            CAN_SendData[0].RemoteFlag = 0;
            CAN_SendData[0].ID = 0x700;

            CAN_SendData[0].SendType = 0;

            Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            CAN_SendData[0].DataLen = 8;
            CAN_SendData[0].Data = new Byte[8];

            if (alldevice_checkBox.Checked)
                CAN_SendData[0].Data[0] = (byte)0xff;
            else
                CAN_SendData[0].Data[0] = (byte)ID;

            CAN_SendData[0].Data[1] = 0x01;
            CAN_SendData[0].Data[2] = 0;
            CAN_SendData[0].Data[3] = 0;
            CAN_SendData[0].Data[4] = 0;
            CAN_SendData[0].Data[5] = 0;
            CAN_SendData[0].Data[6] = 0;
            CAN_SendData[0].Data[7] = 0;

            CAN_SendData[0].ExternFlag = 0;
            CAN_SendData[0].RemoteFlag = 0;
            CAN_SendData[0].ID = 0x700;

            CAN_SendData[0].SendType = 0;

            Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            Int32 cab_current = 0;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            CAN_SendData[0].DataLen = 8;
            CAN_SendData[0].Data = new Byte[8];

            if (alldevice_checkBox.Checked)
                CAN_SendData[0].Data[0] = (byte)0xff;
            else
                CAN_SendData[0].Data[0] = (byte)ID;

            CAN_SendData[0].Data[1] = 0x02;

            cab_current = (Int32)(Convert.ToDouble(cab_current_textBox.Text) * 1000);

            CAN_SendData[0].Data[2] = (byte)(cab_current >> 24); 
            CAN_SendData[0].Data[3] = (byte)(cab_current >> 16); 
            CAN_SendData[0].Data[4] = (byte)(cab_current>>8); 
            CAN_SendData[0].Data[5] = (byte)cab_current;

            CAN_SendData[0].Data[6] = 0;
            CAN_SendData[0].Data[7] = 0;

            CAN_SendData[0].ExternFlag = 0;
            CAN_SendData[0].RemoteFlag = 0;
            CAN_SendData[0].ID = 0x700;

            CAN_SendData[0].SendType = 0;

            Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
        }

    
        private void button5_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            CAN_SendData[0].DataLen = 8;
            CAN_SendData[0].Data = new Byte[8];

            if (alldevice_checkBox.Checked)
                CAN_SendData[0].Data[0] = (byte)0xff;
            else
                CAN_SendData[0].Data[0] = (byte)ID;

            CAN_SendData[0].Data[1] = 0x06;
            CAN_SendData[0].Data[2] = 0x00;
            CAN_SendData[0].Data[3] = 0;
            CAN_SendData[0].Data[4] = 0;
            CAN_SendData[0].Data[5] = 0;
            CAN_SendData[0].Data[6] = 0;
            CAN_SendData[0].Data[7] = 0;

            CAN_SendData[0].ExternFlag = 0;
            CAN_SendData[0].RemoteFlag = 0;
            CAN_SendData[0].ID = 0x700;

            CAN_SendData[0].SendType = 0;

            Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
        }  
        
        private void button6_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            CAN_SendData[0].DataLen = 8;
            CAN_SendData[0].Data = new Byte[8];

            if (alldevice_checkBox.Checked)
                CAN_SendData[0].Data[0] = (byte)0xff;
            else
                CAN_SendData[0].Data[0] = (byte)ID;

            CAN_SendData[0].Data[1] = 0x06;
            CAN_SendData[0].Data[2] = 0x07;
            CAN_SendData[0].Data[3] = 0;
            CAN_SendData[0].Data[4] = 0;
            CAN_SendData[0].Data[5] = 0;
            CAN_SendData[0].Data[6] = 0;
            CAN_SendData[0].Data[7] = 0;

            CAN_SendData[0].ExternFlag = 0;
            CAN_SendData[0].RemoteFlag = 0;
            CAN_SendData[0].ID = 0x700;

            CAN_SendData[0].SendType = 0;

            Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
        }



        private void new_SN_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(!(char.IsNumber(e.KeyChar))&&e.KeyChar!=(char)8)
            {
                e.Handled=true;
            }
        }

        private void cab_current_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8 && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (cab_current_textBox.Text.Contains('.') && e.KeyChar == '.')
            {
                e.Handled = true;
            }
        }

        private void comboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }

        private void comboBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
            {
                e.Handled = true;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            UInt16 NewID = 0;
            NewID = Convert.ToUInt16(comboBox1.Text);
            if (NewID < 16)
            {
                gNewID = NewID;
                CAN_SendData[0].DataLen = 8;
                CAN_SendData[0].Data = new Byte[8];

                if (alldevice_checkBox.Checked)
                    CAN_SendData[0].Data[0] = (byte)0xff;
                else
                    CAN_SendData[0].Data[0] = (byte)ID;

                CAN_SendData[0].Data[1] = 0x10;
                CAN_SendData[0].Data[2] = (byte)NewID;
                CAN_SendData[0].Data[3] = 0;
                CAN_SendData[0].Data[4] = 0;
                CAN_SendData[0].Data[5] = 0;
                CAN_SendData[0].Data[6] = 0;
                CAN_SendData[0].Data[7] = 0;

                CAN_SendData[0].ExternFlag = 0;
                CAN_SendData[0].RemoteFlag = 0;
                CAN_SendData[0].ID = 0x700;

                CAN_SendData[0].SendType = 0;

               // ID_textBox.Text = ID.ToString();
                Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);

            }
            else
            {
                MessageBox.Show("ID范围0~15!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            UInt32 rate = 0;
            rate = ((UInt32)Convert.ToUInt16(comboBox1.Text) + 1) * 250;
            if ((rate <= 1000)&&(rate >= 250))
            {
                CAN_SendData[0].DataLen = 8;
                CAN_SendData[0].Data = new Byte[8];

                if (alldevice_checkBox.Checked)
                    CAN_SendData[0].Data[0] = (byte)0xff;
                else
                    CAN_SendData[0].Data[0] = (byte)ID;

                CAN_SendData[0].Data[1] = 0x13;
                CAN_SendData[0].Data[2] = (byte)(rate>>8);
                CAN_SendData[0].Data[3] = (byte)rate;
                CAN_SendData[0].Data[4] = 0;
                CAN_SendData[0].Data[5] = 0;
                CAN_SendData[0].Data[6] = 0;
                CAN_SendData[0].Data[7] = 0;

                CAN_SendData[0].ExternFlag = 0;
                CAN_SendData[0].RemoteFlag = 0;
                CAN_SendData[0].ID = 0x700;

                CAN_SendData[0].SendType = 0;

                Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
            }
            else
            {
                MessageBox.Show("波特率范围250~1000之间的常用波特率!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            UInt32 Status;
            ControlCAN.VCI_CAN_OBJ[] CAN_SendData = new ControlCAN.VCI_CAN_OBJ[1];

            UInt64 newSN = 0;
            newSN = Convert.ToUInt64(new_SN_textBox.Text);
            if ((newSN <= 4294967296) && (newSN >= 1))
            {
                CAN_SendData[0].DataLen = 8;
                CAN_SendData[0].Data = new Byte[8];

                if (alldevice_checkBox.Checked)
                    CAN_SendData[0].Data[0] = (byte)0xff;
                else
                    CAN_SendData[0].Data[0] = (byte)ID;

                CAN_SendData[0].Data[1] = 0x11;
                CAN_SendData[0].Data[2] = (byte)(newSN >> 24);
                CAN_SendData[0].Data[3] = (byte)(newSN >> 16);
                CAN_SendData[0].Data[4] = (byte)(newSN >> 8); 
                CAN_SendData[0].Data[5] = (byte)(newSN) ;
                CAN_SendData[0].Data[6] = 0;
                CAN_SendData[0].Data[7] = 0;

                CAN_SendData[0].ExternFlag = 0;
                CAN_SendData[0].RemoteFlag = 0;
                CAN_SendData[0].ID = 0x700;

                CAN_SendData[0].SendType = 0;

                Status = ControlCAN.VCI_Transmit(m_devtype, 0, 0, CAN_SendData, 1);
            }
            else
            {
                MessageBox.Show("SN范围0~4294967296之间!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
