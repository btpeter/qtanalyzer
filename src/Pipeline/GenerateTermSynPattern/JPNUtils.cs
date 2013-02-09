using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateTermSynPattern
{
        class JPNUtils
        {
            /**/
            /// <summary>
            /// 转半角的函数(DBC case)
            /// </summary>
            /// <param name=”input”>任意字符串</param>
            /// <returns>半角字符串</returns>
            ///<remarks>
            ///全角空格为12288，半角空格为32
            ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
            ///</remarks>
            public static string ToDBC(string input)
            {
                char[] c = input.ToCharArray();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == 12288)
                    {
                        c[i] = (char)32;
                        continue;
                    }
                    if (c[i] > 65280 && c[i] < 65375)
                        c[i] = (char)(c[i] - 65248);
                }
                return new string(c);
            }

            //public static bool IsHalfKana(string targetStr)
            //{
            //    string halfKanaList = "ｰﾞﾟｧｨｩｪｫｬｭｮｯｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝ";
            //    foreach (char strItem in targetStr)
            //    {
            //        if (!halfKanaList.Contains(strItem.ToString()))
            //        {
            //            return false;
            //        }
            //    }
            //    return true;
            //}
            //public static string ToHalfKana(string str)
            //{
            //    string strFullKanaListPart1 = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンーャュョァィゥェォッ゛゜";
            //    string strFullKanaListPart2 = "ヴ,ガ,ギ,グ,ゲ,ゴ,ザ,ジ,ズ,ゼ,ゾ,ダ,ヂ,ヅ,デ,ド,バ,ビ,ブ,ベ,ボ,パ,ピ,プ,ペ,ポ";
            //    string strHarfKanaPart1 = "ｳﾞ,ｶﾞ,ｷﾞ,ｸﾞ,ｹﾞ,ｺﾞ,ｻﾞ,ｼﾞ,ｽﾞ,ｾﾞ,ｿﾞ,ﾀﾞ,ﾁﾞ,ﾂﾞ,ﾃﾞ,ﾄﾞ,ﾊﾞ,ﾋﾞ,ﾌﾞ,ﾍﾞ,ﾎﾞ,ﾊﾟ,ﾋﾟ,ﾌﾟ,ﾍﾟ,ﾎﾟ";
            //    string strHalfKanaPart2 = "ｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜｦﾝｰｬｭｮｧｨｩｪｫｯﾞﾟ";
            //    StringBuilder sb = new StringBuilder();
            //    foreach (char charInput in str)
            //    {
            //        if (strFullKanaListPart1.Contains(charInput.ToString()))
            //        {
            //            int index = strFullKanaListPart1.IndexOf(charInput);
            //            sb.Append(strHalfKanaPart2[index]);
            //        }
            //        else if (strFullKanaListPart2.Contains(charInput.ToString()))
            //        {
            //            string[] arrFullKaka = strFullKanaListPart2.Split(',');
            //            string[] arrHalfKana = strHarfKanaPart1.Split(',');
            //            int index = Array.IndexOf(arrFullKaka, charInput.ToString());
            //            sb.Append(arrHalfKana[index]);
            //        }
            //        else
            //        {
            //            sb.Append(charInput);
            //        }
            //    }
            //    return sb.ToString();
            //}
        }
}
