namespace CodeHive.DfaLex.re1
{
    internal class Sub
    {
        public const int MaxSub = 20;

        public int   refs;
        public int   nsub;
        public int[] sub;

        public Sub(int nsub)
        {
            refs = 1;
            this.nsub = nsub;
            sub = new int[MaxSub];
            for (var i = 0; i < sub.Length; i++)
            {
                sub[i] = -1;
            }
        }

        public Sub IncRef()
        {
            refs++;
            return this;
        }

        public Sub DecRef()
        {
            refs--;
            return this;
        }

        public Sub Update(int i, int cp)
        {
            var s = this;
            if (refs > 1)
            {
                var s1 = new Sub(nsub);
                for (var j = 0; j < nsub; j++)
                {
                    s1.sub[j] = sub[j];
                }

                refs--;
                s = s1;
            }

            s.sub[i] = cp;
            return s;
        }
    }
}
