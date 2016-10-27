using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.IO.Compression;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Interactions;

namespace Tefreeca_Unit
{
    public static class WebDriverExtensions
    {
        public static void element_click(IWebDriver Driver, IWebElement ELE)
        {
            Actions A = new Actions(Driver);
            A.Click(ELE).Build().Perform();
        }

        public static IWebElement FindElement(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                WebDriverWait wait2 = new WebDriverWait(driver, new TimeSpan(0, 0, timeoutInSeconds));
                try
                {
                    wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(by));
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
            return driver.FindElement(by);
        }

        public static ReadOnlyCollection<IWebElement> FindElements(this IWebDriver driver, By by, int timeoutInSeconds)
        {
            if (timeoutInSeconds > 0)
            {
                WebDriverWait wait2 = new WebDriverWait(driver, new TimeSpan(0, 0, timeoutInSeconds));
                try
                {
                    wait2.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(by));
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
            return driver.FindElements(by);
        }

        
    }
    
    public class tools
    {
        public static void Decompress(FileInfo fileToDecompress) //압축을 푼다. 간단하다; 경로지정하면
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }
        //http://molis.iptime.org:8800/~bet365/odds.xml.gz
        public string get_gzip_string(string url)// 주소로 부터 gzip 파일을 내려받아 내부에 있는 한개의 파일의 경로를 반환한다.
        {
            string filename = url.Substring(url.LastIndexOf('/') + 1); //url에서 파일명 추출.
            string inner_filename = filename.Replace(".gz", "");
            try
            {
                WebClient Wclient = new WebClient();

                if (System.IO.File.Exists(@filename)) //파일이 있으면 먼저 삭제.
                {
                    try
                    {
                        System.IO.File.Delete(@filename);
                    }
                    catch
                    {
                        return "";
                    }
                }
                if (System.IO.File.Exists(@inner_filename)) //파일이 있으면 먼저 삭제.
                {
                    try
                    {
                        System.IO.File.Delete(@inner_filename);
                    }
                    catch
                    {
                        return "";
                    }
                }

                Wclient.DownloadFile(url, filename);
                Decompress(new FileInfo(filename));
                //압축해제.

                //텍스트 추출..
            }
            catch
            {
                return "";
            }
            return inner_filename;
        }

        public string convert_west_EUR_to_eng(string team_name) //서유럽 국가의 알파벳 표기법중 문제가 되는걸 알파벳으로 바꾼다.
        {
            string[] before = new string[] { "À", "Á", "Â", "Ã", "Ä", "Å", "Æ", "Ç", "È", "É", "Ê", "Ë", "Ì", "Í", "Î", "Ï", "Ð", "Ñ", "Ò", "Ó", "Ô", "Õ", "Ö", "Ø", "Ù", "Ú", "Û", "Ü", "Ý", "ß", "à", "á", "â", "ã", "ä", "å", "ç", "è", "é", "ê", "ë", "ì", "í", "î", "ï", "ñ", "ò", "ó", "ô", "õ", "ö", "ø", "ù", "ú", "û", "ü", "ý", "ÿ" };
            string[] after_ = new string[] { "A", "A", "A", "A", "A", "A", "AE", "C", "E", "E", "E", "E", "I", "I", "I", "I", "D", "N", "O", "O", "O", "O", "O", "O", "U", "U", "U", "U", "Y", "b", "a", "a", "a", "a", "a", "a", "c", "e", "e", "e", "e", "i", "i", "i", "i", "n", "o", "o", "o", "o", "o", "o", "u", "u", "u", "u", "y", "y" };
            for (int i = 0; i < before.Length; i++)
            {
                team_name = team_name.Replace(before[i], after_[i]);
            }
            return team_name;
        }

        public int string_count_of_contain(string source, string find_value)
        {
            int count = 0;
            int pointer = 0;
            int thisloc = 0;
            while (true)
            {
                try
                {
                    thisloc = source.IndexOf(find_value, pointer);
                    if (thisloc == -1)
                        break;
                    else
                    {
                        count++;
                    }
                    pointer = thisloc + find_value.Length;
                }
                catch
                {
                    break;
                }
            }
            return count;
        }

        public string get_answer(string txturl) //인터넷에서 소스를 가져와 반환하는 함수 동적 처리에서는 동작하지 않는다..
        {
            HttpWebRequest hwr = (HttpWebRequest)HttpWebRequest.Create(txturl);
            HttpWebResponse hwrp = (HttpWebResponse)hwr.GetResponse();
            WebHeaderCollection whc = hwrp.Headers;

            string header = null, html = null;
            for (int i = 0; i < whc.Count; i++)
                header += whc.GetKey(i) + " = " + whc.GetKey(i) + "\r\n";

            Stream strm = hwrp.GetResponseStream();
            StreamReader sr = new StreamReader(strm, Encoding.UTF8);

            //Stream strm = hwrp.GetResponseStream();
            //StreamReader sr = new StreamReader(strm, Encoding.Default);


            while (sr.Peek() > -1)
                html += sr.ReadLine() + "\r\n";

            sr.Close();
            strm.Close();

            return html;
        }

        public int month_convert(string mon)
        {
            switch (mon)
            {
                case "Jan": return 1;
                case "Feb": return 2;
                case "Mar": return 3;
                case "Apr": return 4;
                case "May": return 5;
                case "Jun": return 6;
                case "Jul": return 7;
                case "Aug": return 8;
                case "Sep": return 9;
                case "Oct": return 10;
                case "Nov": return 11;
                case "Dec": return 12;
                default: return 0;
            }
        }
        public ArrayList Array_connect(ArrayList a1, ArrayList a2) //어레이 리스트 두개를 이어 붙인다.
        {
            for (int i = 0; i < a2.Count; i++)
            {
                a1.Add(a2[i]);
            }
            return a1;
        }

        public float base_score(string value) //+ - 가 포함된 배당 혹은 vs 혹은 "" 의 기준점을 적절히 float 형태로 바꿔 반환.
        {
            float miner = (float)1;

            if (value.Contains("-")) //마이너스가 포함되 있으면
                miner = (float)-1;

            value = value.Replace(" ", "").Replace("+", "").Replace("-", "");
            if (value == "" || value.Contains("vs") || value.Contains("VS"))
                value = "0";

            miner *= (float.Parse(value));

            return miner;
        }

        public float fraction_txt(string value) //분수로 된 배당을 데시멀로 바꿔줌 텍스트로 된 경우의 분수.
        {
            if (!value.Contains("/"))//분수 표기가 없으면 0반환.
                return 0;
            string[] split_value = value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            return float.Parse(split_value[0]) / float.Parse(split_value[1]);
        }

        public float fraction(string value) //분수로 된 배당을 데시멀로 바꿔줌 특문으로 된 분수.
        {
            value = value.Replace("&nbsp;", "");
            float miner = (float)1;
            if (value.Contains("-")) //마이너스가 포함되 있으면
            {
                miner = (float)-1;
            }
            value = value.Replace(" ", "").Replace("+", "").Replace("-", "");

            if (value.Contains("½"))
            {
                value = value.Replace("½", "");
                if (value == "")
                    value = "0";
                miner *= (float.Parse(value) + (float)0.5);
            }
            else if (value.Contains("¼"))
            {
                value = value.Replace("¼", "");
                if (value == "")
                    value = "0";
                miner *= (float.Parse(value) + (float)0.25);
            }
            else if (value.Contains("¾"))  //-0.75
            {
                value = value.Replace("¾", "");
                if (value == "")
                    value = "0";
                miner *= (float.Parse(value) + (float)0.75);
            }
            else
            {
                if (value == "")
                    value = "0";


                if (value.Contains("VS") || value.Contains("vs"))
                    value = "0";
                miner *= (float.Parse(value));
            }

            return miner;
        }
        public ArrayList partag_notcomma(string source, string tag, string element, string value) //값이 작은 따옴표 없이 존재할때..
        {
            ArrayList result = new ArrayList();

            int offset = 0, count = 0;
            while ((source.IndexOf("<" + tag, offset) >= 0) && ((count++) < 6000))
            {
                int S = source.IndexOf("<" + tag, offset);
                int E = source.IndexOf(">", S + ("<" + tag).Length) + 1;

                if (E > S)
                {
                    string teg_t = source.Substring(S, E - S);

                    if (teg_t.IndexOf(element + "=" + value + " ") >= 0 || teg_t.IndexOf(element + "=" + value + ">") >= 0)
                    {
                        int sub_offset = 0, depth = 1, sub_S = 0, sub_E = 0;
                        while (depth > 0 && (count++) < 6000)
                        {
                            sub_S = source.IndexOf("<" + tag, E + sub_offset);
                            sub_E = source.IndexOf("</" + tag + ">", E + sub_offset);

                            if ((sub_S > sub_E) || (sub_S < 0))
                            {
                                depth--;
                                sub_offset = (sub_E - E + ("</" + tag + ">").Length);
                            }
                            else
                            {
                                depth++;
                                sub_offset = (sub_S - E + ("<" + tag).Length);
                            }
                            if (depth < 1)
                                result.Add((object)(source.Substring(E, sub_E - E)));
                            offset = sub_E;
                        }
                    }
                    else
                        offset = E;
                }
            }
            return result;
        }

        public ArrayList partag_value(string source, string tag, string element, string value, string target, string delimiter) //원하는 테그와 테크값의 다른 테그 값을 가져옴.
        {
            ArrayList result = new ArrayList();

            int offset = 0, count = 0;
            while ((source.IndexOf("<" + tag, offset) >= 0) && ((count++) < 3000))
            {
                int S = source.IndexOf("<" + tag, offset);
                int E = source.IndexOf(">", S + ("<" + tag).Length) + 1;

                if (E > S)
                {
                    string teg_t = source.Substring(S, E - S);

                    if (teg_t.IndexOf(element + "=" + delimiter + value + delimiter + " ") >= 0)
                    {
                        string findtag = find_xml(teg_t, target + "=" + delimiter, delimiter + " ");
                        if (findtag != null)
                        {
                            result.Add((object)findtag);
                        }
                        int sub_offset = 0, depth = 1, sub_S = 0, sub_E = 0;
                        while (depth > 0 && (count++) < 3000)
                        {
                            sub_S = source.IndexOf("<" + tag, E + sub_offset);
                            sub_E = source.IndexOf("</" + tag + ">", E + sub_offset);

                            if ((sub_S > sub_E) || (sub_S < 0))
                            {
                                depth--;
                                sub_offset = (sub_E - E + ("</" + tag + ">").Length);
                            }
                            else
                            {
                                depth++;
                                sub_offset = (sub_S - E + ("<" + tag).Length);
                            }
                            //result.Add((object)(source.Substring(E, sub_E - E)));
                            offset = sub_E;
                        }
                    }
                    else
                        offset = E;
                }
            }
            return result;
        }

        public ArrayList partag_value(string source, string tag, string element, string value, string target) //원하는 테그와 테크값의 다른 테그 값을 가져옴.
        {
            return partag_value(source, tag, element, value, "\"");
        }

        public ArrayList gettag(string source, string tag_name) //그냥 태그리스트.
        {
            ArrayList result = new ArrayList();
            int offset = 0;

            while (source.IndexOf("<" + tag_name, offset) >= 0)
            {
                int S = source.IndexOf("<" + tag_name, offset);
                int E = source.IndexOf(">", S + ("<" + tag_name).Length) + 1;

                if (E > S)
                {
                    int sub_offset = 0, depth = 1, sub_S = 0, sub_E = 0;

                    while (depth > 0)
                    {
                        sub_S = source.IndexOf("<" + tag_name, E + sub_offset);
                        sub_E = source.IndexOf("</" + tag_name + ">", E + sub_offset);


                        if ((sub_S > sub_E) || (sub_S < 0))
                        {
                            depth--;
                            sub_offset = (sub_E - E + ("</" + tag_name + ">").Length);
                        }
                        else
                        {
                            depth++;
                            sub_offset = (sub_S - E + ("<" + tag_name).Length);
                        }

                        if (depth < 1)
                        {
                            try
                            {
                                result.Add((object)(source.Substring(E, sub_E - E)));
                            }
                            catch
                            {
                                return result;
                            }
                        }
                        offset = sub_E;
                    }
                }
            }
            return result;
        }

        public ArrayList partag(string source, string tag, string element, string value, string delimiter, int volume)
        {
            ArrayList result = new ArrayList();
            if (source == null)
                return result;
            int offset = 0, count = 0;
            while ((source.IndexOf("<" + tag, offset) >= 0) && ((count++) < 99999))
            {
                int S = source.IndexOf("<" + tag, offset);
                int E = source.IndexOf(">", S + ("<" + tag).Length) + 1;

                if (E > S)
                {
                    string teg_t = source.Substring(S, E - S);

                    if (teg_t.IndexOf(element + "=" + delimiter + value + delimiter) >= 0)
                    {
                        int sub_offset = 0, depth = 1, sub_S = 0, sub_E = 0;

                        while (depth > 0 && (count++) < 99999)
                        {
                            sub_S = source.IndexOf("<" + tag, E + sub_offset);
                            sub_E = source.IndexOf("</" + tag + ">", E + sub_offset);


                            if ((sub_S > sub_E) || (sub_S < 0))
                            {
                                depth--;
                                sub_offset = (sub_E - E + ("</" + tag + ">").Length);
                            }
                            else
                            {
                                depth++;
                                sub_offset = (sub_S - E + ("<" + tag).Length);
                            }
                            if (depth < 1)
                                result.Add((object)(source.Substring(E, sub_E - E)));
                            offset = sub_E;

                            if (result.Count >= volume && volume != 0)
                                return result;
                        }
                    }
                    else
                        offset = E;
                }
            }
            return result;
        }

        public ArrayList partag(string source, string tag, string element, string value, int volume)
        {
            return partag(source, tag, element, value, "\"", volume);
        }

        public ArrayList partag(string source, string tag, string element, string value, string delimiter)
        {
            return partag(source, tag, element, value, delimiter, 0);
        }

        public ArrayList partag(string source, string tag, string element, string value)
        {
            return partag(source, tag, element, value, "\"");
        }

        public string get_convert_number(int num) //숫자중 날짜 표기를 위해 한자리 숫자는 
        {
            if (num > 9) //2자리 숫자면,
            {
                return num.ToString();
            }
            else
            {
                return "0" + num.ToString();
            }
        }

        public ArrayList Parse_XML(string source, string s_txt, string e_txt)
        {
            ArrayList result = new ArrayList();
            int i = 0, j, index_s = 0; //현재 처리된 값.
            string S = s_txt;
            string E = e_txt;
            string R = null;

            while (index_s < source.Length)
            {
                i = source.IndexOf(S, index_s);

                if (i < 0)
                    break; //검색문이 없으면, 종료.

                j = source.IndexOf(E, i); //끝자리 문자의 시작자리 찾음.

                if (j < 0)
                    break; //검색문이 없으면, 종료.


                i += S.Length; //검색문의 실제 내용의 자리를 알아냄

                R = source.Substring(i, j - i); //공백제거한 서브스트링을 가져옴.

                result.Add((object)R); //추가

                index_s = j + E.Length; //다음 검색자리 찾음

                if (source.Length < (index_s + E.Length))
                {
                    break;
                }
            }

            return result;
        }
        public ArrayList Parse_XML(string xml, string element)
        {
            ArrayList result = new ArrayList();
            int sub_close = 0, i = 0, j, index_s = 0; //현재 처리된 값.
            string S = "<" + element;
            string E = "</" + element + ">";
            string R = null;

            while (index_s < xml.Length)
            {
                int x = xml.IndexOf(E, index_s);
                i = xml.IndexOf(S, index_s) + S.Length; //검색문의 실제 내용의 자리를 알아냄
                sub_close = xml.IndexOf(">", i);
                string sub_string = xml.Substring(i, sub_close - i).Replace(" ", ""); //공백제거한 서브스트링을 가져옴.

                j = xml.IndexOf(E, 1 + index_s); //> table_result.IndexOf('¿', i + 1) ? //짧은걸 반환
                if ((j - sub_close - 1) < 0)
                    break;
                R = xml.Substring(sub_close + 1, j - sub_close - 1);
                index_s = xml.IndexOf(E, index_s) + E.Length; //검색이 끝난 후 시작할 다음 자리.

                //string dsdf = xml.Substring(index_s, 20);
                result.Add((object)R);
            }

            return result;
        }

        public string find_xml(string source, string obj)//db테이블을 읽어와서 태그의 요소를 찾아 반환
        {
            int i, j;
            string S = "<" + obj + ">";
            string E = "</" + obj + ">";
            string R = null;
            try
            {
                i = source.IndexOf(S) + S.Length; //검색문의 실제 내용의 자리를 알아냄
                j = source.IndexOf(E, i + 1); //> table_result.IndexOf('¿', i + 1) ? //짧은걸 반환
                R = source.Substring(i, j - i);
            }
            catch
            {
                //MessageBox.Show("XML 파싱오류", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return R; //못찾으면 null반환
        }
        public string find_xml(string source, string s_txt, string e_txt)//db테이블을 읽어와서 태그의 요소를 찾아 반환
        {
            int i, j;
            string S = s_txt;
            string E = e_txt;
            string R = null;
            try
            {
                i = source.IndexOf(S) + S.Length; //검색문의 실제 내용의 자리를 알아냄
                j = source.IndexOf(E, i + 1); // 두번째 검색문을 첫번째 검색문끝의 다음자리부터 검색.
                R = source.Substring(i, j - i);
            }
            catch
            {

            }
            return R; //못찾으면 null반환
        }
        public string find_xml(string source, string s_txt, string e_txt, int start_index)//db테이블을 읽어와서 태그의 요소를 찾아 반환
        {
            int i, j;
            string S = s_txt;
            string E = e_txt;
            string R = null;
            try
            {
                i = source.IndexOf(S, start_index) + S.Length; //검색문의 실제 내용의 자리를 알아냄
                j = source.IndexOf(E, start_index, i + 1); // 두번째 검색문을 첫번째 검색문끝의 다음자리부터 검색.
                R = source.Substring(i, j - i);
            }
            catch
            {

            }
            return R; //못찾으면 null반환
        }
        
        //반올림
        public double ToHalfAdjust(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);
            return dValue > 0 ? System.Math.Floor((dValue * dCoef) + 0.5) / dCoef :
                 System.Math.Ceiling((dValue * dCoef) - 0.5) / dCoef;
        }

        //올림
        public double ToRoundUp(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);
            return dValue > 0 ? System.Math.Ceiling(dValue * dCoef) / dCoef :
                 System.Math.Floor(dValue * dCoef) / dCoef;
        }

        //내림
        public double ToRoundDown(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);
            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                 System.Math.Ceiling(dValue * dCoef) / dCoef;
        }

        //반올림
        public float ToHalfAdjust(float dValue, int iDigits)
        {
            return (float)ToHalfAdjust((double)dValue, iDigits);
        }

        //올림
        public float ToRoundUp(float dValue, int iDigits)
        {
            return (float)ToRoundUp((double)dValue, iDigits);
        }

        //내림
        public float ToRoundDown(float dValue, int iDigits)
        {
            return (float)ToRoundDown((double)dValue, iDigits);
        }
    }

    public class hash_pro
    {
        public bool is_duple(string value)
        {
            return is_duple(value, 1);
        }

        public void reset() //모든 데이터를 초기화 시킨다. 처음상태로..
        {
            h_list.Clear();
            h_hash.Clear();
        }

        private ArrayList h_list = new ArrayList();
        private Hashtable h_hash = new Hashtable();

        private bool is_duple(string value, int index_length)//true면 중복이란 말이고 false 면 중복이 아닌 새로운것이란 말.
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return true;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                    return true;
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return is_duple(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    try
                    {
                        table.Add(table.Count, value);
                    }
                    catch { }
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                h_hash.Add(key, new Hashtable());
                h_list.Add(h_hash[key]);

                Hashtable table = (Hashtable)h_hash[key];
                try
                {
                    table.Add(0, value);
                }
                catch { }

                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }

        public bool has_element(string value) //단지 있는지 없는지만 계산
        {
            return has_element(value, 1);
        }

        private bool has_element(string value, int index_length)
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return true;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                    return true;
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return has_element(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }
        public bool remove_element(string value) //단지 있는지 없는지만 계산
        {
            return remove_element(value, 1);
        }

        private bool remove_element(string value, int index_length) //지우는데 성공하면 t 실패하면 f
        {
            if (value == null) //값이 재대로 안들어오면 무조건 중복으로 간주, 버리도록 유도.
                return false;

            string key = value.Substring(0, index_length);

            if (h_hash.Contains(key)) //해당 키가 있으면, 해당 키의 해시에 이메일이 있는지 검사,
            {
                Hashtable table = (Hashtable)h_hash[key];
                if (table.ContainsValue(value)) //이메일을 찾으면 찾았다고 하고 탈출
                {
                    for (int i = 0; i < table.Count; i++)
                    {
                        if (table[i].ToString() == value)
                        {
                            table.Remove(i);
                            break;
                        }
                    }
                    return true;
                }
                else //못찾으면, 사이즈 검사해서 더 깊이 찾아 들어갈지, 검사..
                    if (table.Count > 1000) //맥시멈 사이즈 이상이면, 또다른 테이블이 존재할 수 있으므로 검사.
                    return has_element(value, index_length + 1);
                else //맥시멈이 아니면 더이상 다른 테이블이 없으므로 거기에 추가하고 종료
                {
                    return false;
                }
            }
            else //없으면, 처음 새롭게 만들어지는 테이블이다. 그러므로 테이블 만들고 이멜 추가하고 바로 종료.
            {
                return false;
            } //t 면 이메일을 찾았다는 말, f면 못찾았다는 말.
        }
    }
}
