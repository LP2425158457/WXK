using System;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace LP.WXK.K3.Common.Utils
{
    /// <summary>
    /// RSA加密解密帮助类
    /// 提供RSA密钥生成、加密、解密功能
    /// </summary>
    public class RsaHelper
    {
        private static readonly Encoding Encoding_UTF8 = Encoding.UTF8;

        /// <summary>
        /// RSA密钥对结构
        /// </summary>
        public struct RsaKeyPair
        {
            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }

        /// <summary>
        /// 生成RSA密钥对
        /// </summary>
        /// <returns>RSA密钥对</returns>
        public RsaKeyPair GenerateKeyPair()
        {
            RsaKeyPairGenerator keyGenerator = new RsaKeyPairGenerator();

            RsaKeyGenerationParameters param = new RsaKeyGenerationParameters(
                Org.BouncyCastle.Math.BigInteger.ValueOf(3),
                new SecureRandom(),
                1024,
                25);

            keyGenerator.Init(param);
            AsymmetricCipherKeyPair keyPair = keyGenerator.GenerateKeyPair();
            AsymmetricKeyParameter publicKey = keyPair.Public;
            AsymmetricKeyParameter privateKey = keyPair.Private;

            SubjectPublicKeyInfo subjectPublicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
            PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);

            Asn1Object asn1ObjectPublic = subjectPublicKeyInfo.ToAsn1Object();
            byte[] publicInfoByte = asn1ObjectPublic.GetEncoded("UTF-8");
            Asn1Object asn1ObjectPrivate = privateKeyInfo.ToAsn1Object();
            byte[] privateInfoByte = asn1ObjectPrivate.GetEncoded("UTF-8");

            return new RsaKeyPair()
            {
                PublicKey = Convert.ToBase64String(publicInfoByte),
                PrivateKey = Convert.ToBase64String(privateInfoByte)
            };
        }

        /// <summary>
        /// 获取公钥参数
        /// </summary>
        /// <param name="keyBase64">Base64编码的公钥</param>
        /// <returns>公钥参数</returns>
        private AsymmetricKeyParameter GetPublicKeyParameter(string keyBase64)
        {
            keyBase64 = keyBase64.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            byte[] publicInfoByte = Convert.FromBase64String(keyBase64);
            AsymmetricKeyParameter pubKey = PublicKeyFactory.CreateKey(publicInfoByte);
            return pubKey;
        }

        /// <summary>
        /// 获取私钥参数
        /// </summary>
        /// <param name="keyBase64">Base64编码的私钥</param>
        /// <returns>私钥参数</returns>
        private AsymmetricKeyParameter GetPrivateKeyParameter(string keyBase64)
        {
            keyBase64 = keyBase64.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            byte[] privateInfoByte = Convert.FromBase64String(keyBase64);
            AsymmetricKeyParameter priKey = PrivateKeyFactory.CreateKey(privateInfoByte);
            return priKey;
        }

        /// <summary>
        /// 私钥加密
        /// </summary>
        /// <param name="data">加密内容</param>
        /// <param name="privateKey">私钥（Base64编码）</param>
        /// <returns>Base64编码的加密结果</returns>
        public string EncryptByPrivateKey(string data, string privateKey)
        {
            IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());

            try
            {
                engine.Init(true, GetPrivateKeyParameter(privateKey));
                byte[] byteData = Encoding_UTF8.GetBytes(data);
                var ResultData = engine.ProcessBlock(byteData, 0, byteData.Length);
                return Convert.ToBase64String(ResultData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 私钥解密
        /// </summary>
        /// <param name="data">待解密的内容（Base64编码）</param>
        /// <param name="privateKey">私钥（Base64编码）</param>
        /// <returns>解密后的明文</returns>
        public string DecryptByPrivateKey(string data, string privateKey)
        {
            data = data.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());

            try
            {
                engine.Init(false, GetPrivateKeyParameter(privateKey));
                byte[] byteData = Convert.FromBase64String(data);
                var ResultData = engine.ProcessBlock(byteData, 0, byteData.Length);
                return Encoding_UTF8.GetString(ResultData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 公钥加密
        /// </summary>
        /// <param name="data">加密内容</param>
        /// <param name="publicKey">公钥（Base64编码）</param>
        /// <returns>Base64编码的加密结果</returns>
        public string EncryptByPublicKey(string data, string publicKey)
        {
            IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());

            try
            {
                engine.Init(true, GetPublicKeyParameter(publicKey));
                byte[] byteData = Encoding_UTF8.GetBytes(data);
                var ResultData = engine.ProcessBlock(byteData, 0, byteData.Length);
                return Convert.ToBase64String(ResultData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 公钥解密
        /// </summary>
        /// <param name="data">待解密的内容（Base64编码）</param>
        /// <param name="publicKey">公钥（Base64编码）</param>
        /// <returns>解密后的明文</returns>
        public string DecryptByPublicKey(string data, string publicKey)
        {
            data = data.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());

            try
            {
                engine.Init(false, GetPublicKeyParameter(publicKey));
                byte[] byteData = Convert.FromBase64String(data);
                var ResultData = engine.ProcessBlock(byteData, 0, byteData.Length);
                return Encoding_UTF8.GetString(ResultData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
