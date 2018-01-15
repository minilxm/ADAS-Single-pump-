using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace PrecisionMeasurement
{
    public class Encrypt
    {
        private static readonly string DESKey = "a1b2c3d4";

        /// <summary>  
        /// DES加密方法  
        /// </summary>  
        /// <param name="strPlaintext">明文</param>  
        /// <param name="DESKey">密钥，长度为8</param>  
        /// <param name="DESKey">向量</param>  
        /// <returns>密文</returns>  
        public static string DESEncrypt(string strPlaintext)
        {
            if (DESKey.Length != 8)
            {
                throw new ArgumentException("DESKey Length must be equals 8!");
            }
            try
            {
                //把密钥转换成字节数组
                byte[] bytesDESKey = ASCIIEncoding.ASCII.GetBytes(DESKey);
                //把向量转换成字节数组
                byte[] bytesDESIV = ASCIIEncoding.ASCII.GetBytes(DESKey);
                //声明1个新的DES对象  
                DESCryptoServiceProvider desEncrypt = new DESCryptoServiceProvider();
                //开辟一块内存流
                MemoryStream msEncrypt = new MemoryStream();
                //把内存流对象包装成加密流对象  
                CryptoStream csEncrypt = new CryptoStream(msEncrypt,
                    desEncrypt.CreateEncryptor(bytesDESKey, bytesDESIV), CryptoStreamMode.Write);
                //把加密流对象包装成写入流对象  
                StreamWriter swEncrypt = new StreamWriter(csEncrypt);
                //写入流对象写入明文  
                swEncrypt.WriteLine(strPlaintext);
                //写入流关闭  
                swEncrypt.Close();
                //加密流关闭  
                csEncrypt.Close();
                //把内存流转换成字节数组，内存流现在已经是密文了  
                byte[] bytesCipher = msEncrypt.ToArray();
                //内存流关闭  
                msEncrypt.Close();
                //把密文字节数组转换为字符串，并返回  
                return Convert.ToBase64String(bytesCipher);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>  
        /// DES解密方法  
        /// </summary>  
        /// <param name="strCiphertext">密文</param>  
        /// <param name="DESKey">密钥，长度为8</param>  
        /// <param name="DESKey">向量</param>  
        /// <returns>明文</returns>  
        public static string DESDecrypt(string strCiphertext)
        {
            if (DESKey.Length != 8)
            {
                throw new ArgumentException("DESKey Length must be equals 8!");
            }
            try
            {
                //把密钥转换成字节数组  
                byte[] bytesDESKey = ASCIIEncoding.ASCII.GetBytes(DESKey);
                //把向量转换成字节数组  
                byte[] bytesDESIV = ASCIIEncoding.ASCII.GetBytes(DESKey);
                //把密文转换成字节数组  
                byte[] bytesCipher = Convert.FromBase64String(strCiphertext);
                //声明1个新的DES对象  
                DESCryptoServiceProvider desDecrypt = new DESCryptoServiceProvider();
                //开辟一块内存流，并存放密文字节数组
                MemoryStream msDecrypt = new MemoryStream(bytesCipher);
                //把内存流对象包装成解密流对象  
                CryptoStream csDecrypt = new CryptoStream(msDecrypt, desDecrypt.CreateDecryptor(bytesDESKey, bytesDESIV),
                    CryptoStreamMode.Read);
                //把解密流对象包装成读出流对象  
                StreamReader srDecrypt = new StreamReader(csDecrypt);
                //明文=读出流的读出内容  
                string strPlainText = srDecrypt.ReadLine();
                //读出流关闭  
                srDecrypt.Close();
                //解密流关闭  
                csDecrypt.Close();
                //内存流关闭  
                msDecrypt.Close();
                //返回明文  
                return strPlainText;
            }
            catch
            {
                return string.Empty;
            }
        }

    }//end class
}
