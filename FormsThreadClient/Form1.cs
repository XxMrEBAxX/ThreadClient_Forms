using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;

namespace FormsThreadClient
{
    public partial class Form1 : Form
    {
        TcpClient client;
        NetworkStream networkStream;
        StreamReader reader;
        StreamWriter writer;
        Thread received;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("25.40.224.197"),13000);
                client = new TcpClient();
                client.Connect(iPEndPoint);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
                Application.Exit();
                return;
            }
            networkStream = client.GetStream();
            reader = new StreamReader(networkStream);
            writer = new StreamWriter(networkStream);
            // 쓰레드로 입력과 동시에 서버에서 문자를 계속 받아옴.
            received = new Thread(new ThreadStart(() =>
            {
                string m_line = "";
                TextBox textBox = textBox2;
                while (true)
                {
                    if (!client.Connected) break;
                    try
                    {
                        m_line = reader.ReadLine();
                    }
                    catch(Exception)
                    {
                        Application.Exit();
                    }
                    // 크로스 스레드 접근 오류 해결 (디버그에서만 오류남)
                    CrossThreadWorkHelper.AppendTextCrossThread(textBox, m_line + "\r\n");
                }
            }));
            received.Start();
            received.IsBackground = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendMessage();
        }
        private void SendMessage()
        {
            if (textBox1.Text.Equals("")) return;
            try
            {
                writer.WriteLine(string.Format(textBox1.Text));
                writer.Flush();
                textBox2.AppendText(string.Format("[나]: {0}" + "\r\n", textBox1.Text));
                textBox1.Text = "";
            }
            catch (Exception error)
            {
                MessageBox.Show(error.ToString());
                Application.Exit();
                return;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            ClientExit();
        }
        void ClientExit()
        {
            if (received != null)
                received.Abort();
            if (writer != null)
            {
                writer.WriteLine("$exit"); //서버에게 종료 메시지
                writer.Flush();
                writer.Close();
            }
            if (reader != null)
                reader.Close();
            if (networkStream != null)
                networkStream.Close();
            if (client != null)
                client.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                SendMessage();
            }
        }
    }
    // 크로스 스레드 참조 해결 클래스
    public static class CrossThreadWorkHelper
    {
        public static void AppendTextCrossThread(this TextBox textBox, string appendText)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new MethodInvoker(delegate ()
                {
                    textBox.AppendText(appendText);
                }));
            }
            else
            {
                textBox.AppendText(appendText);
            }
        }
    }
}
