using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Drawing;
using QRKinectDecode.Zxing.Helper;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace QRKinectDecode
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private KinectSensor Kino = null;

        // Parte variables del usercontrol
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private ColorImageFormat lastImageFormat = ColorImageFormat.Undefined;

        private DateTime ultimoframe = DateTime.MinValue;

        // Array de bytes inicial que captura Kinect PARA dibujar imagen en pantalla a 30fps
        private byte[] pixelData;

        // BitMap a visualizar en PANTALLA
        private WriteableBitmap outputImage;

        // Opcion 3
        System.Timers.Timer timerTimers;
        object objectBloqueo = new object();

        private int anchuraimagen;
        private int alturaimagen;
        private int longitudtotalimagen;

		public MainWindow()
		{
			InitializeComponent();

			if(KinectSensor.KinectSensors.Count == 0)
			{
				throw new NotImplementedException();
			}

			Kino = KinectSensor.KinectSensors[0];
			if(!Kino.IsRunning)
			{
				Kino.Start();
				Kino.ElevationAngle = 0;
			}

			Kino.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            Kino.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(ColorImageReady);

            cmbTiposCodificacion.Items.Add(DecodificadorQR.formatos.Codigo_QR.ToString());
            cmbTiposCodificacion.Items.Add(DecodificadorQR.formatos.EAN_13.ToString());
            cmbTiposCodificacion.Items.Add(DecodificadorQR.formatos.EAN_8.ToString());
            cmbTiposCodificacion.SelectedIndex = 0;

            // lanzamos timer cada segundo
            timerTimers = new System.Timers.Timer(1000);
            timerTimers.Elapsed += new System.Timers.ElapsedEventHandler(timerTimers_Elapsed);
            timerTimers.Start();
  		}

        void timerTimers_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.Print("Ejecucion tick timer. Thread : '" + Thread.CurrentThread.ManagedThreadId + "'. TimeStamp :'" + DateTime.Now.ToLongTimeString() + "'.");
            metodoTimerNormal();
        }

        void metodoTimerNormal()
        {
            // Este bloqueo evita que dos posibles ticks del timer ejecuten a la vez el codigo
             if (Monitor.TryEnter(objectBloqueo))
             {
                try
                {
                     // Este sentencia evita que el primer tick se ejecute antes de la existencia de la imagen.
                    if (lastImageFormat != ColorImageFormat.Undefined)
                    {
                        Bitmap bmap = new Bitmap(anchuraimagen, alturaimagen, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                        BitmapData bmapdata = bmap.LockBits(new System.Drawing.Rectangle(0, 0, anchuraimagen, alturaimagen), ImageLockMode.WriteOnly, bmap.PixelFormat);
                        IntPtr ptr = bmapdata.Scan0;
                        Marshal.Copy(this.pixelData, 0, ptr, longitudtotalimagen);
                        bmap.UnlockBits(bmapdata);
                        DecodificadorQR dec = new DecodificadorQR();

                        bmap.RotateFlip(RotateFlipType.RotateNoneFlipX);

                        string res = string.Empty;
                        // miramos valor combo en pantalla 
                        string contenidoCombo = string.Empty;
                        // Estamos en un hilo de un timer, en el threading pool. Para actualizar la interfaz hay que usar dispatcher.
                        Dispatcher dispacherUI2 = cmbTiposCodificacion.Dispatcher;
                        dispacherUI2.Invoke(new Action(delegate()
                        {
                            contenidoCombo = cmbTiposCodificacion.SelectedValue.ToString();
                        }));                
                        switch (contenidoCombo)
                        {
                            case "Codigo_QR":
                                res = dec.DecodificaQRZxing(bmap, DecodificadorQR.formatos.Codigo_QR);
                                break;

                            case "EAN_13":
                                res = dec.DecodificaQRZxing(bmap, DecodificadorQR.formatos.EAN_13);
                                break;

                            case "EAN_8":
                                res = dec.DecodificaQRZxing(bmap, DecodificadorQR.formatos.EAN_8);
                                break;
                        }
                        if (!string.IsNullOrEmpty(res))
                        {
                            // Estamos en un hilo de un timer, en el threading pool. Para actualizar la interfaz hay que usar dispatcher.
                            Dispatcher dispacherUI = lstResultadoQR.Dispatcher;
                            dispacherUI.Invoke(new Action(delegate()
                            {
                                lstResultadoQR.Items.Add(res);
                            }));
                            Debug.Print("Código '" + res + "' decodificado.");
                        }
                        else
                            Debug.Print("No hay codigo a decodificar.");

                        bmap.Dispose();
                        ultimoframe = DateTime.Now;
                    } 
                }
                finally
                {
                    Monitor.Exit(objectBloqueo);
                }
             }         
        }
                              
        /// <summary>
        /// Este evento se lanza aprox. 30 fps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorImageReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    // We need to detect if the format has changed.
                    bool haveNewFormat = this.lastImageFormat != imageFrame.Format;

                    if (haveNewFormat)
                    {
                        // Dimensionamos array byte[] que recoge frame Kinect (la primera vez).
                        this.pixelData = new byte[imageFrame.PixelDataLength];
                        this.anchuraimagen = imageFrame.Width;
                        this.alturaimagen = imageFrame.Height;
                        this.longitudtotalimagen = imageFrame.PixelDataLength;
                    }

                    // recogemos imagen.
                    imageFrame.CopyPixelDataTo(this.pixelData);
    
                    // Aquí se dimensiona el WriteableBitmap. s a WPF construct that enables resetting the Bits of the image.
                    // This is more efficient than creating a new Bitmap every frame.
                    if (haveNewFormat)
                    {
                        kinectImage.Visibility = Visibility.Visible;
                        this.outputImage = new WriteableBitmap(
                                imageFrame.Width,
                                imageFrame.Height,
                                96,  // DpiX
                                96,  // DpiY
                                PixelFormats.Bgr32,
                                null);

                        kinectImage.Source = this.outputImage;
                    }

                    this.outputImage.WritePixels(
                            new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                            this.pixelData,
                            imageFrame.Width * Bgr32BytesPerPixel,
                            0);
                   	                                 
                     this.lastImageFormat = imageFrame.Format;
                                                    
                    //UpdateFrameRate();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
		{
            
			Kino.Stop();
			Kino = null;

            timerTimers.Stop();
		}
	}
}
