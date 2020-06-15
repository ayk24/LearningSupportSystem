using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MyUtil;

namespace server
{
	public partial class Form1 : Form
	{
		Bitmap pintedImg_;
		bool isDrawing_;
		Point oldPos_;
		Point currentPos_;
		Point startPos_;

        Pen pen_red;
        Pen pen_black;
        Pen pen_blue;
        Pen pen_green;
        Pen pen_yellow;
        Pen pen_white;

        HandleServer server_;

        public int pen_color = 1;
        private Image image;

        public Form1()
		{
            InitializeComponent();
            pintedImg_ = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            isDrawing_ = false;
            pen_red = new Pen(Color.FromArgb(255, 255, 0, 0), 5);
            pen_black = new Pen(Color.FromArgb(255, 0, 0, 0), 5);
            pen_blue = new Pen(Color.FromArgb(255, 0, 120, 255), 5);
            pen_green = new Pen(Color.FromArgb(255, 21, 184, 0), 5);
            pen_yellow = new Pen(Color.FromArgb(255, 255, 216, 0), 5);
            pen_white = new Pen(Color.FromArgb(255, 255, 255, 255), 30);
        }

		//ピクチャーボックス上のマウスダウンイベント
		private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left)
			{
				currentPos_.X = e.X;
				currentPos_.Y = e.Y;
				oldPos_ = currentPos_;
				startPos_ = currentPos_;
				isDrawing_ = true;
			}
		}

		//ピクチャーボックス上のマウスムーブイベント
		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			if (server_ != null && isDrawing_)
			{
				using (var g = Graphics.FromImage(pintedImg_))
				{
                    switch (pen_color)
                    {
                        case 1:
                            g.DrawLine(pen_black, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                        case 2:
                            g.DrawLine(pen_red, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                        case 3:
                            g.DrawLine(pen_blue, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                        case 4:
                            g.DrawLine(pen_yellow, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                        case 5:
                            g.DrawLine(pen_green, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                        case 6:
                            g.DrawLine(pen_white, oldPos_.X, oldPos_.Y, currentPos_.X, currentPos_.Y);
                            break;
                    }

					oldPos_ = currentPos_;
					currentPos_.X = e.X;
					currentPos_.Y = e.Y;
					pictureBox1.Image = pintedImg_; //描画更新

					server_.sendPaintInfo(startPos_, currentPos_, pen_color);
					startPos_ = currentPos_;

				}
			}
		}

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }


        //ピクチャーボックス上のマウスアップイベント
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				isDrawing_ = false;
			}
		}

        //フォームのロードイベント
        private void Form1_Load(object sender, EventArgs e)
		{
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(server_ != null)
			{
				server_.IsWorking = false;
			}
		}

		private void btnStart_Click(object sender, EventArgs e)
		{
			int port = 0;
			bool canConvert = int.TryParse(textPort.Text, out port);
			if (canConvert == false)
			{
				MessageBox.Show("ポート番号に数値を入力してください");
				return;
			}

			try
			{
				server_ = new HandleServer();
				server_.start(port);
				server_.RecivedMessageEvent = (string name, string text) =>
				{
					if (this.InvokeRequired)//別スレッドから呼び出されたとき Invokeして呼びなおす
					{
						this.Invoke(new Action(() => server_.RecivedMessageEvent(name, text)), null);
						return;
					}

					textInfo.Text += (name +  " >> " + text + "\r\n");
                    textInfo.HideSelection = false;
                    textInfo.AppendText("\n");
                };

                server_.RecivedImageEvent = (string name, byte[] image) =>
                {
                    if (this.InvokeRequired)//別スレッドから呼び出されたとき Invokeして呼びなおす
                    {
                        this.Invoke(new Action(() => server_.RecivedImageEvent(name, image)), null);
                        return;
                    }

                    pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                    Image img = Util.ByteArrayToImage(image);
                    pictureBox2.Image = img;
                };

                pintedImg_ = new Bitmap(pictureBox1.Width, pictureBox1.Height);
				isDrawing_ = false;
				ChagenButtonState();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			server_.IsWorking = false;
			ChagenButtonState();
		}

		private void ChagenButtonState()
		{
			btnStart.Enabled = !btnStart.Enabled;
			btnStop.Enabled = !btnStop.Enabled;
		}

        private void sendM_Click(object sender, EventArgs e)
        {
            if (server_ == null || !server_.IsWorking)
            {
                MessageBox.Show("通信していません");
                return;
            }
            server_.sendMessage(sendText.Text);
            server_.RecivedMessageEvent("自分", sendText.Text);
            sendText.Text = "";
        }

        //チャットのテキストボックス内でEnterを押したら送信ボタンを押したことにする
        private void send(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendM.PerformClick();
            }
        }

        private void sendM_Click_1(object sender, EventArgs e)
        {
            if (server_ == null || !server_.IsWorking)
            {
                MessageBox.Show("通信していません");
                return;
            }
            server_.sendMessage(sendText.Text);
            server_.RecivedMessageEvent("自分", sendText.Text);
            sendText.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pen_color = 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            pen_color = 6;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pen_color = 2;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pen_color = 3;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            pen_color = 4;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            pen_color = 5;
        }
    }
}
