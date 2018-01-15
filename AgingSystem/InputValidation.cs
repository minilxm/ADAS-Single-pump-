using System.Text.RegularExpressions;
using System;

namespace  AgingSystem
{
    public class InputValidation
    {
        /// <summary>
        /// Whether the char is letter or digit
        /// </summary>
        /// <param name="keyChar">char to be validated</param>
        /// <returns>true: letter or digital; false:otherwise</returns>
        public static bool IsLetterOrDigit(char keyChar)
        {
            bool isLetterOrDigit = false;
            //is digital
            if (keyChar >= 48 && keyChar <= 57)
            {
                isLetterOrDigit = true;
            }
            else if (keyChar >= 97 && keyChar <= 122)
            {//lower letter
                isLetterOrDigit = true;
            }
            else if (keyChar >= 65 && keyChar <= 90)
            {//upper letter
                isLetterOrDigit = true;
            }

            return isLetterOrDigit;
        }
        /// <summary>
        /// Whether the string contains only letter and digit
        /// </summary>
        /// <param name="keyChar">string to be validated</param>
        /// <returns>true: only letter and digit; false:otherwise</returns>
        public static bool HasOnlyLetterOrDigit(string chars)
        {
            bool isLetterOrDigit = true;
            int length = chars.Length;
            if (length <= 0)
            {
                isLetterOrDigit = false;
            }
            else
            {
                foreach (char c in chars)
                {
                    isLetterOrDigit = IsLetterOrDigit(c);
                    if (!isLetterOrDigit)
                    {
                        break;
                    }
                }
            }
            return isLetterOrDigit;
        }
          
        public static bool HasOnlyLetterOrDigitByRegex(string chars)
        {
            string strReg = @"^([a-zA-Z]|\d){0,8}[\d]*$";
            Regex reg = new Regex(strReg);
            bool bRet = reg.IsMatch(chars, 0);
            return bRet;
        }


        /// <summary>
        /// check a input string, is decimal with 1or2 decimalPlace
        /// </summary>
        /// <param name="strNumber">input number</param>
        /// <param name="decimalPlace">decimal Places,1 OR 2</param>
        /// <returns></returns>
        public static bool IsDecimalWith1Or2Place(string strNumber, int decimalPlace)
        {
            string szDecimalPlace = decimalPlace.ToString();
            string strWith0decimalPlace = @"^(?!0+(?:\.0+)?$)(?:[1-9]\d*|0)(?:\d{0})?$";
            string strWith1decimalPlace = @"^(?!0+(?:\.0+)?$)(?:[1-9]\d*|0)(?:\.\d{1})?$";
            string strWith2decimalPlace = @"^(?!0+(?:\.0+)?$)(?:[1-9]\d*|0)(?:\.\d{1,"+szDecimalPlace+"})?$";
            Regex reg = null;
            if (decimalPlace==0)
            {
                reg = new Regex(strWith0decimalPlace);
            }
            else if(decimalPlace==1)
                reg = new Regex(strWith1decimalPlace);
            else
                reg = new Regex(strWith2decimalPlace);
            bool bRet = reg.IsMatch(strNumber, 0);
            return bRet;
        }

        /// <summary>
        /// 28 bytes bar code (The First eight bytes is consisted by Numbers and letters) 
        /// </summary>
        /// <param name="strBarCode"></param>
        /// <returns></returns>
        public static bool IsBarCode(string strBarCode)
        {
            string strReg = @"^[\w\d]{8}[\d]{20}$";
            Regex reg = new Regex(strReg);
            bool bRet = reg.IsMatch(strBarCode, 0);
            return bRet;
        }

        /// <summary>
        /// check textbox value
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="max"></param>
        /// <param name="decimalPlace"></param>
        /// <param name="integerPartCounts">integer part number not > 4</param>
        public static void ValidateTextBox(System.Windows.Controls.TextBox textbox, float max, int decimalPlace, int integerPartCounts=4)
        {
            if (textbox == null)
            {
                return;
            }
            string strText = textbox.Text;
            if (string.IsNullOrEmpty(strText))
            {
                return;
            }
            if (strText == "0")
            {
                return;
            }
            if (decimalPlace == 0 && strText[strText.Length - 1] == '.')
            {
                textbox.Text = strText.Substring(0, strText.Length - 1);
                textbox.SelectionStart = textbox.Text.Length;
                return;
            }

            int length = strText.Length;
            int dotIndex = strText.IndexOf('.');
            if (strText[length-1]=='.') // if the last char is a dot
            {
                if (length==1)          // if a dot and no other number before  
                {
                    textbox.Text = strText.Substring(0, length - 1);
                    textbox.SelectionStart = textbox.Text.Length;
                    return;
                }
                if (dotIndex != length - 1) // if has more than one dot
                {
                    textbox.Text = strText.Substring(0, length - 1);
                    textbox.SelectionStart = textbox.Text.Length;
                    return;
                }
                else if (length >= integerPartCounts + 1) //if more than integerPartCounts
                {
                    textbox.Text = strText.Substring(0, length - 1);
                    textbox.SelectionStart = textbox.Text.Length;
                    return;
                }
                else
                {
                    try
                    {
                        if (float.Parse(textbox.Text.Substring(0, textbox.Text.Length - 1)) > max)
                            textbox.Text = strText.Substring(0, length - 1);
                        else
                            return;
                    }
                    catch
                    {
                        textbox.Text = strText.Substring(0, length - 1);
                        return;
                    }
                }
            }
            try
            {
                if (float.Parse(strText.Trim().Trim('.')) > max) // if > max
                {
                    textbox.Text = strText.Substring(0, length - 1);
                    textbox.SelectionStart = textbox.Text.Length;
                    return;
                }

                if (strText.Length>integerPartCounts 
                    &&
                    strText.Substring(0, strText.IndexOf('.')).Length >= integerPartCounts)
                    //(int)(float.Parse(strText)) >= integerPartCounts
                    /*strText.Substring(0, integerPartCounts).Trim('.').Length >= integerPartCounts*/
                {
                    textbox.Text = strText.Substring(0, length - 1);
                }
            }
            catch
            {
                textbox.Text = strText.Substring(0, length - 1);
                textbox.SelectionStart = textbox.Text.Length;
                return;
            }
            if (!IsDecimalWith1Or2Place(strText, decimalPlace)) // if not a Decimal
            {
                textbox.Text = strText.Substring(0, length - 1);
                textbox.SelectionStart = textbox.Text.Length;
                return;
            }
        }

        /// <summary>
        /// 验证文本框数字（最小，最大）
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="min"></param>
        /// <param name="max">最大值里面的小数并不代表真实的小数位</param>
        /// <param name="decimalPlace">小数有几位</param>
        //public static bool ValidateTextBox(string text, float min, float max, int decimalPlace)
        //{
        //    string strText = text;
        //    if (string.IsNullOrEmpty(strText))
        //    {
        //        return true;
        //    }
        //    //先把数字拆开
        //    string szFormat = "F" + decimalPlace.ToString();
        //    string szMax = max.ToString(szFormat);
        //    string szMaxIntPart = ((int)max).ToString() + ".";
        //    string szMaxDecimalPart = szMax.Replace(szMaxIntPart, "");
        //    string szMin = min.ToString(szFormat).PadLeft(szMax.Length, '0');
        //    string szMinIntPart = szMin.Substring(0, szMaxIntPart.Length);
        //    string szMinDecimalPart = szMin.Replace(szMinIntPart, "");

        //    if (Convert.ToInt32(strText.TrimEnd('.')) >= Convert.ToInt32(szMinIntPart.TrimEnd('.'))
        //   && Convert.ToInt32(strText.TrimEnd('.')) <= Convert.ToInt32(szMaxIntPart.TrimEnd('.'))
        //   )
        //        return true;
        //    else
        //        return false;

        //}

        /// <summary>
        /// check bar number
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="barLength"></param>
        public static void ValidateTextBoxBarNumber(System.Windows.Controls.TextBox textbox, int barLength = 28)
        {
            if (textbox == null)
            {
                return;
            }
            string strText = textbox.Text;
            if (string.IsNullOrEmpty(strText))
            {
                return;
            }
            int length = strText.Length;
            if (!HasOnlyLetterOrDigitByRegex(strText)) // if not bar code
            {
                textbox.Text = strText.Substring(0, length - 1);
                textbox.SelectionStart = textbox.Text.Length;
                return;
            }
            if (length >= barLength 
                && !IsBarCode(textbox.Text)
                )
            {
                textbox.Text = strText.Substring(0, length - 1);
                textbox.SelectionStart = textbox.Text.Length;
                return;
            }

        }

    }//end InputValidation
}