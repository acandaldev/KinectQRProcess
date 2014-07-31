using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.google.zxing;
using com.google.zxing.qrcode;
using com.google.zxing.common;
using System.Drawing;
using System.Collections;
using System.Diagnostics;

namespace QRKinectDecode.Zxing.Helper
{
    public class DecodificadorQR
    {

        private com.google.zxing.qrcode.QRCodeReader _dec;
        private com.google.zxing.oned.EAN13Reader _decEAN13;
        private com.google.zxing.oned.EAN8Reader _decEAN8;

        public DecodificadorQR()
        {
            _dec = new QRCodeReader();
            _decEAN13 = new com.google.zxing.oned.EAN13Reader();
            _decEAN8 = new com.google.zxing.oned.EAN8Reader();
        }

        public enum formatos
        {
            Codigo_QR = 0,
            EAN_13 = 1,
            EAN_8 = 2
        }

        /// <summary>
        /// Intenta reconocer un codigo QR usando libreria Zxing en el BitMap pasado como parámetro.
        /// </summary>
        /// <param name="bitmap">Imagen BitMap</param>
        /// <returns>
        /// 
        /// CORRECTO : devuelve un string con el valor del codigo QR.
        /// INCORRECTO : cadena vacía.
        /// 
        /// </returns>
        public string DecodificaQRZxing(Bitmap bitmap,formatos formato)
        {
            Hashtable hint = new Hashtable();
            BarcodeFormat de = null; 
            var rgb = new RGBLuminanceSource(bitmap, bitmap.Width, bitmap.Height);
            var hybrid = new com.google.zxing.common.HybridBinarizer(rgb);
            com.google.zxing.BinaryBitmap binBitmap = new com.google.zxing.BinaryBitmap(hybrid);
            string decodedString = string.Empty;
            hint.Add(com.google.zxing.DecodeHintType.TRY_HARDER, true);


            switch (formato)
            {
                case formatos.Codigo_QR : 
                    de = BarcodeFormat.QR_CODE;
                    hint.Add(DecodeHintType.POSSIBLE_FORMATS, de);
                    try
                    {
                        decodedString = _dec.decode(binBitmap, hint).Text;
                    }
                    catch (Exception)
                    { }
                    break;

                case formatos.EAN_13 :
                    de = BarcodeFormat.EAN_13;
                    hint.Add(DecodeHintType.POSSIBLE_FORMATS, de);
                    try
                    {
                        decodedString = _decEAN13.decode(binBitmap, hint).Text;
                    }
                    catch (Exception)
                    { }
                    break;

                case formatos.EAN_8 :
                        de = BarcodeFormat.EAN_8;
                        hint.Add(DecodeHintType.POSSIBLE_FORMATS, de);
                        try
                        {
                            decodedString = _decEAN8.decode(binBitmap, hint).Text;
                        }
                        catch (Exception)
                        { }
                    break;

            }             
            return decodedString;
        }      
    }
}
