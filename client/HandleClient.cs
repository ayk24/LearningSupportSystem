using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows;

using System.Net;           //IPEndPointを使うため
using System.Threading;     //スレッドを使うため
using System.Net.Sockets;   //ソケットを使うため

using MyUtil;   //自作dllを使用するため

namespace client
{
    class HandleClient
    {
        protected Socket socket_;
        protected Object SyncSocket_ = new Object();
        protected string Name_;
        public bool IsWorking   //falseしてスレッドを止める
        {
            get;
            set;
        }

        public delegate void UpdatePictureBox(PaintInfo info);
        public UpdatePictureBox PaintEvent;

        public delegate void RecivedMessage(string name, string text);
        public RecivedMessage RecivedMessageEvent;

        //★増えたよ！
        public delegate void RecivedImage(string name, byte[] image);
        public RecivedImage RecivedImageEvent;

        public bool start(string name, string ip, int port)
        {
            var ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket_.Connect(ipep);
                socket_.Send(System.Text.Encoding.UTF8.GetBytes(name)); //名前の送信

                var bytesFrom = new byte[65536];
                int readBytes = socket_.Receive(bytesFrom);
                var recvedData = new byte[readBytes];
                Array.Copy(bytesFrom, 0, recvedData, 0, readBytes);
                var res = System.Text.Encoding.UTF8.GetString(recvedData);
                if (res == "OK")
                {
                    Name_ = name;
                    var ctThread = new Thread(this.doWork);
                    ctThread.Start();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        public void sendMessage(string msg)
        {
            if (IsWorking)
            {
                lock (SyncSocket_)
                {
                    //チャットデータをシリアライズ
                    var sendData = new ChatData()
                    {
                        Name = Name_,
                        Text = msg
                    };
                    var jsonString = Util.Serialize(sendData);
                    var sendToBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

                    //独自パケット作成
                    var mergedArray = Util.MergeByteArrays(new List<byte[]>
                    {
                        Util.ToBigEndianArray((ulong)sendToBytes.Length + 1,4),
                        new byte[]{Util.TypeChat},
                        sendToBytes
                    });

                    //サーバに送信
                    lock (SyncSocket_)
                    {
                        socket_.Send(mergedArray);
                    }
                }
            }
        }

        // ★変えたよ！
        public void sendImage(byte[] tbuf)
        {
            if (IsWorking)
            {
                lock (SyncSocket_)
                {
                    //チャットデータをシリアライズ
                    var sendData = new ImageData()
                    {
                        Name = Name_,
                        Byte = tbuf
                    };


                    var jsonString = Util.Serialize(sendData);
                    var sendToBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

                    //独自パケット作成
                    var mergedArray = Util.MergeByteArrays(new List<byte[]>
                    {
                        Util.ToBigEndianArray((ulong)sendToBytes.Length + 1,4),
                        new byte[]{Util.TypeImage},
                        sendToBytes
                    });

                    //サーバに送信
                    lock (SyncSocket_)
                    {
                        socket_.Send(mergedArray);
                    }
                }
            }
        }


        protected void doWork()
        {
            IsWorking = true;
            while (IsWorking)
            {
                lock (SyncSocket_)
                {
                    bool poolState = socket_.Poll(1000, SelectMode.SelectRead);
                    if (poolState && socket_.Available == 0)
                    {
                        break;
                    }

                    if (poolState)
                    {
                        int DataType;
                        byte[] BytesFrom = Util.ReadSocketSteram(socket_, out DataType);
                        int DataSize = BytesFrom.Length;

                        //データタイプによって処理を切り替える
                        if (DataType == Util.TypePos)
                        {
                            var info = Util.DeSerialize<PaintInfo>(BytesFrom);
                            PaintEvent(info);
                        }
                        else if (DataType == Util.TypeChat)
                        {
                            var data = Util.DeSerialize<ChatData>(BytesFrom);
                            RecivedMessageEvent(data.Name, data.Text);
                        }
                        // ★増えたよ！
                        else if (DataType == Util.TypeImage)
                        {
                            var image = Util.DeSerialize<ImageData>(BytesFrom);
                            RecivedImageEvent(image.Name, image.Byte);
                        }
                    }
                }
            }
            socket_.Close();
        }
    }
}
