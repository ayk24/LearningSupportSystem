using System;
using System.Collections.Generic;
using System.Text;

//これらを使うために以下の参照の追加が必要
//System.Runtime.Serialization,System.ServiceModel,System.ServiceModel.Web
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using System.IO;        //MemoryStreamを使うため
using System.Drawing;   //Point構造体を使うため

using System.Net.Sockets;
namespace MyUtil
{
    public class Util
    {
        public static readonly byte TypePos = 0;
        public static readonly byte TypeChat = 1;

        // バイト配列をImageオブジェクトに変換
        public static Image ByteArrayToImage(byte[] b)
        {
            var imgconv = new ImageConverter();
            Image img = (Image)imgconv.ConvertFrom(b);
            return img;
        }

        // Imageオブジェクトをバイト配列に変換
        public static byte[] ImageToByteArray(Image img)
        {
            var imgconv = new ImageConverter();
            byte[] b = (byte[])imgconv.ConvertTo(img, typeof(byte[]));
            return b;
        }

        public static byte[] ReadSocketSteram(Socket opendSock, out int DataType)
        {
            DataType = -1;
            if (opendSock == null || opendSock.Connected == false)
            {
                return null;
            }
            var temp = new Byte[1024];

            //データの大きさの取得と、データ格納配列の確保
            var SizeArray = new byte[4];
            opendSock.Receive(SizeArray, 0, 4, SocketFlags.None);
            int DataSize = Util.ToIntFromBigEndianArray(SizeArray) - 1;
            var BytesFrom = new byte[DataSize];

            //データタイプの取得
            opendSock.Receive(temp, 0, 1, SocketFlags.None);
            DataType = temp[0];

            //データの読み込み
            int RemainingSize = DataSize;
            int ReadDataSize = 0;
            int BufferOffset = 0;
            while (RemainingSize > 0)
            {
                int ReadTempSize = (RemainingSize > temp.Length) ? temp.Length : RemainingSize;
                ReadDataSize = opendSock.Receive(temp, 0, ReadTempSize, SocketFlags.None);
                Array.Copy(temp, 0, BytesFrom, BufferOffset, ReadDataSize);
                BufferOffset += ReadDataSize;
                RemainingSize -= ReadDataSize;
            }

            return BytesFrom;
        }

        //★増えてるよ！
        public static readonly byte TypeImage = 2;

        public static string Serialize(object graph)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(graph.GetType());
                serializer.WriteObject(stream, graph);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T DeSerialize<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }

        //整数をビッグエンディアンのバイト配列に変換
        public static byte[] ToBigEndianArray(ulong value, int byteNum)
        {
            if (byteNum <= 0 || byteNum > 8)
            {
                return null;
            }

            var byteArray = new byte[byteNum];
            for (int i = byteNum; i > 0; --i)
            {
                byteArray[byteNum - i] = (byte)(((value & (ulong)((ulong)0xff << ((i - 1) * 8)))) >> ((i - 1) * 8));
            }

            return byteArray;
        }

        //ビッグエンディアンのバイト配列をint型に変換
        public static int ToIntFromBigEndianArray(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0 || byteArray.Length > 4)
            {
                return 0;
            }

            int num = 0;
            for (int i = 0; i < byteArray.Length; ++i)
            {
                num += byteArray[byteArray.Length - 1 - i] << (i * 8);
            }
            return num;
        }

        //バイト配列の結合
        public static byte[] MergeByteArrays(IList<byte[]> arrays)
        {
            if (arrays == null || arrays.Count == 0)
            {
                return null;
            }

            //結合後のバイト数を求める
            int bytes = 0;
            foreach (var array in arrays)
            {
                bytes += array.Length;
            }

            //リストに格納されたバイト配列を、一つのバイト配列に結合
            var mergedArray = new byte[bytes];
            int basePos = 0;
            foreach (var array in arrays)
            {
                Array.Copy(array, 0, mergedArray, basePos, array.Length);
                basePos += array.Length;
            }

            return mergedArray;
        }
    }

    [DataContract]
    public class PaintInfo
    {
        [DataMember]
        public Point StartPos { get; set; }

        [DataMember]
        public Point EndPos { get; set; }

        [DataMember]
        public int Pen_color { get; set; }

    }

    [DataContract]
    public class ChatData
    {
        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public String Text { get; set; }
    }

    // ★これも増えた！
    [DataContract]
    public class ImageData
    {
        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public byte[] Byte { get; set; }
    }
}
