using System;
using System.Collections.Generic;
using System.Text;

namespace CILExamining.Obfuscations.Utilities {
    public sealed class RenameUtility {
        private Random random;
        public Random Random => random;
        private HashSet<string> used;
        private int index;
        private const string set = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";

        public RenameUtility() {
            this.random = new Random();
            this.used = new HashSet<string>();
            this.index = 32;
        }

        public RenameUtility(int seed) {
            this.random = new Random(seed);
            this.used = new HashSet<string>();
            this.index = 32;
        }

        public RenameUtility(int seed, IEnumerable<string> exclude) {
            this.random = new Random(seed);
            this.used = new HashSet<string>(exclude);
            this.index = 32;
        }

        public string GetBase64Encode(string from) {
            throw new NotImplementedException();
        }

        public string GetMD5Hash(string from) {
            throw new NotImplementedException();
        }

        public string GetObfuscated(bool allowSame) {
            return "What the fuck?" + GetRandomString(false, 128);
        }

        public string GetRandomString(bool allowSame) {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < random.Next(64) + 1; i++)
                sb.Append(set[random.Next(set.Length)]);
            if (!allowSame && used.Contains(sb.ToString()))
                return GetRandomString(false);
            else
                return sb.ToString();
        }

        public string GetRandomString(bool allowSame, int length) {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
                sb.Append(set[random.Next(set.Length)]);
            if (!allowSame && used.Contains(sb.ToString()))
                return GetRandomString(false);
            else
                return sb.ToString();
        }
    }
}
