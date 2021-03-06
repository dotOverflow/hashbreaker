﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace HashBreaker
{
    public class Cryption
    {

        public delegate void Callback(params object[] objects);

        public Thread[] threads;

        private int hashLength;

        public string type, method;

        private HashAlgorithm[] hashAlgos;

        public Cryption(int hashLength = 10, string type = "MD5", string method = "bruteforce") {
            this.threads = new Thread[5];

            this.hashLength = hashLength;

            this.type = type;
            this.method = method;

            this.hashAlgos = new HashAlgorithm[6];

            this.hashAlgos[0] = MD5.Create();
            this.hashAlgos[1] = SHA1.Create();
            this.hashAlgos[2] = SHA256.Create();
            this.hashAlgos[3] = SHA384.Create();
            this.hashAlgos[4] = SHA512.Create();
            this.hashAlgos[5] = MD4.Create();
        }

        public string toBase64(string str) {
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(str));
        }

        public string fromBase64(string str) {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }

        public bool checkHash(string hash) {
            hash = hash.Trim();
            if (string.IsNullOrWhiteSpace(hash)) return false;
            string pattern = "";
            if (type.Equals("MD5")) pattern = "^[a-fA-F0-9]{32}$";
            else if (type.Equals("MD4")) pattern = "^[a-fA-F0-9]{40}$";
            else if (type.Equals("SHA-1")) pattern = "^[a-fA-F0-9]{40}$";
            else if (type.Equals("SHA-256")) pattern = "^[a-fA-F0-9]{64}$";
            else if (type.Equals("SHA-384")) pattern = "^[a-fA-F0-9]{96}$";
            else if (type.Equals("SHA-512")) pattern = "^[a-fA-F0-9]{128}$";
            else if (type.Equals("ODO")) pattern = "^[a-zA-Z0-9-]{94}$";
            else if (type.Equals("BASE64")) return (hash.Length % 4 == 0) && Regex.IsMatch(hash, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
            return Regex.IsMatch(hash, pattern, RegexOptions.Compiled);
        }

        public string toODO(string chain)  {
            this.type = "SHA-1";
            string sha1Hash = this.encrypt(chain);
            this.type = "MD5";
            string fmd5Hash = this.encrypt(sha1Hash);
            string smd5Hash = Utils.reverse(this.encrypt(fmd5Hash));
            string hash = Utils.randomStr(6) + fmd5Hash + smd5Hash + Utils.randomStr(6);
            hash = Utils.reverse(hash);
            hash = Utils.addIndexes(4, hash, "-");
            return hash;
        }

        public string encrypt(string plain) {
            byte[] data = null;
            if (type.Equals("MD5")) data = this.hashAlgos[0].ComputeHash(Encoding.UTF8.GetBytes(plain));
            else if (type.Equals("SHA-1")) data = this.hashAlgos[1].ComputeHash(Encoding.UTF8.GetBytes(plain));
            else if (type.Equals("SHA-256")) data = this.hashAlgos[2].ComputeHash(Encoding.UTF8.GetBytes(plain));
            else if (type.Equals("SHA-384")) data = this.hashAlgos[3].ComputeHash(Encoding.UTF8.GetBytes(plain));
            else if (type.Equals("SHA-512")) data = this.hashAlgos[4].ComputeHash(Encoding.UTF8.GetBytes(plain));
            else if (type.Equals("MD4")) data = this.hashAlgos[5].ComputeHash(Encoding.UTF8.GetBytes(plain));
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                stringBuilder.Append(data[i].ToString("x2"));
            return stringBuilder.ToString();
        }

        public void decrypt(string hash, bool odo, params Callback[] callbacks) {
            if (callbacks.Length < 3) throw new Exception("We need 4 callbacks in decrypt method!");
            callbacks[0]();
            this.threads[0] = new Thread(() => {
                if (odo) {
                    hash = hash.Replace("-", string.Empty);
                    hash = hash.Substring(6, hash.Length - 12);
                }
                string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789éèàùìîôêçâ !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
                int startTime = Environment.TickCount;
                for (int length = 1; length <= this.hashLength; ++length) {
                    callbacks[1](length);
                    StringBuilder stringBuilder = new StringBuilder(new string('a', length));
                    while (true) {
                        string value = stringBuilder.ToString();
                        string hashedVal = odo ? toODO(value) : encrypt(value);
                        if (odo) {
                            hashedVal = hashedVal.Replace("-", string.Empty); ;
                            hashedVal = hashedVal.Substring(6, hashedVal.Length - 12);
                        }
                        if (hashedVal.Equals(hash)) {
                            int delay = Environment.TickCount - startTime;
                            callbacks[2](delay, value);
                            abortThread(0);
                            callbacks[3](value);
                        }
                        if (value.All(item => item == '~'))
                            break;
                        for (int i = length - 1; i >= 0; --i) {
                            if (stringBuilder[i] != '~') {
                                stringBuilder[i] = chars.ToCharArray()[chars.LastIndexOf(stringBuilder[i]) + 1];
                                break;
                            } else stringBuilder[i] = chars.ToCharArray()[0];
                        }
                    }
                }
            });
            this.threads[0].Start();
        }

        public bool abortThread(int thread) {
            if (this.threads[thread].IsAlive)
                this.threads[thread].Abort();
            return this.threads[thread].IsAlive;
        }

    }
}
