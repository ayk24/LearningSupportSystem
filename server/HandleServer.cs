using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;           //IPEndPointを使うため
using System.Threading;     //スレッドを使うため
using System.Net.Sockets;   //ソケットを使うため
using System.Drawing;       //point構造体を使うため

using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using System.Collections;

using MyUtil;	//自作dllを使用するため
namespace server
{
    public class HandleServer
    {
        protected Object SyncClientSockets_ = new Object();
        protected Socket serverSocket_;
        public Dictionary<string, Socket> clinetList_ = new Dictionary<string, Socket>();
        public string theme = "";
        public delegate void RecivedMessage(string name, string text);
        public RecivedMessage RecivedMessageEvent;

        // ★変えたよ！
        public delegate void RecivedImage(string name, byte[] image);
        public RecivedImage RecivedImageEvent;

        public bool IsWorking   //falseにするとサーバスレッドを止める
        {
            get;
            set;
        }

        public void start(int port)
        {
            var ipp = new IPEndPoint(IPAddress.Any, port);
            serverSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket_.Bind(ipp);
            serverSocket_.Listen(10);

            var ctThread = new Thread(this.doWork);
            ctThread.Start();
        }

        protected void doWork()
        {
            IsWorking = true;
            var bytesFrom = new byte[2048];
            while (IsWorking)
            {
                bool poolState = serverSocket_.Poll(1, SelectMode.SelectRead);
                if (poolState)
                {
                    Socket client = serverSocket_.Accept();

                    int readBytes = client.Receive(bytesFrom);
                    var recivedData = new byte[readBytes];
                    Array.Copy(bytesFrom, 0, recivedData, 0, readBytes);
                    var userName = System.Text.Encoding.UTF8.GetString(recivedData);

                    if (userName == "" || clinetList_.ContainsKey(userName))
                    {
                        client.Send(System.Text.Encoding.UTF8.GetBytes("NG"));
                        client.Close();
                    }
                    else
                    {
                        client.Send(System.Text.Encoding.UTF8.GetBytes("OK"));
                        clinetList_.Add(userName, client);
                    }
                }

                foreach (var name in clinetList_.Keys)
                {
                    poolState = clinetList_[name].Poll(1, SelectMode.SelectRead);
                    if (poolState)
                    {
                        int DataType;
                        byte[] BytesFrom = Util.ReadSocketSteram(clinetList_[name], out DataType);
                        int DataSize = BytesFrom.Length;

                        if (DataType == Util.TypeChat)
                        {
                            var chatData = Util.DeSerialize<ChatData>(BytesFrom);
                            sendChatData(chatData.Name, chatData.Text);
                            RecivedMessageEvent(chatData.Name, chatData.Text);
                        }

                        else if (DataType == Util.TypeImage)
                        {
                            var imageData = Util.DeSerialize<ImageData>(BytesFrom);
                            sendImageData(imageData.Name, imageData.Byte);
                            RecivedImageEvent(imageData.Name, imageData.Byte);
                        }
                    }
                }
            }
            serverSocket_.Close();
            foreach (var name in clinetList_.Keys)
            {
                clinetList_[name].Close();
            }
            clinetList_.Clear();
        }

        public void sendChatData(string sourceName, string text)
        {
            //チャットデータをシリアライズ
            var sendData = new ChatData()
            {
                Name = sourceName,
                Text = text
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

            //全てのクライアントに送信
            lock (SyncClientSockets_)
            {
                foreach (var name in clinetList_.Keys)
                {
                    if (name != sourceName)
                    {
                        clinetList_[name].Send(mergedArray);
                    }
                }
            }
        }

        public void sendImageData(string sourceName, byte[] image)
        {
            //イメージデータをシリアライズ
            var sendData = new ImageData()
            {
                Name = sourceName,
                Byte = image
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

            //全てのクライアントに送信
            lock (SyncClientSockets_)
            {
                foreach (var name in clinetList_.Keys)
                {
                    if (name != sourceName)
                    {
                        clinetList_[name].Send(mergedArray);
                    }
                }
            }
        }


        public void sendPaintInfo(Point startPos, Point endPos, int pen_color)
        {
            //座標データをシリアライズ
            var sendData = new PaintInfo()
            {
                StartPos = startPos,
                EndPos = endPos,
                Pen_color = pen_color
            };
            var jsonString = Util.Serialize(sendData);
            var sendToBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            //独自パケット作成
            var mergedArray = Util.MergeByteArrays(new List<byte[]>
            {
                Util.ToBigEndianArray((ulong)sendToBytes.Length + 1,4),
                new byte[]{Util.TypePos},
                sendToBytes
            });

            //全てのクライアントに送信
            lock (SyncClientSockets_)
            {
                foreach (var name in clinetList_.Keys)
                {
                    clinetList_[name].Send(mergedArray);
                }
            }
        }

        public void sendMessage(string msg)
        {
            if (IsWorking)
            {
                lock (SyncClientSockets_)
                {
                    //チャットデータをシリアライズ
                    var sendData = new ChatData()
                    {
                        Name = "host",
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

                    //すべてのクライアントに送信
                    lock (SyncClientSockets_)
                    {
                        foreach (var name in clinetList_.Keys)
                        {
                            if (name != "host")
                            {
                                clinetList_[name].Send(mergedArray);
                            }
                        }
                    }
                }
            }
        }
    }
}
