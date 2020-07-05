using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  public class CtrlPush : Image
    {
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Propiedad para definir cual del las imagenes asociadas al control es la que se muestra
    public static readonly DependencyProperty IdxImgProperty =
        DependencyProperty.Register("IdxImg", typeof(int), typeof(CtrlPush), new PropertyMetadata(-1, OnIdxImgChange ));

    public int IdxImg
      {
      get { return (int)GetValue(IdxImgProperty); }
      set { SetValue(IdxImgProperty, value); }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    /// Evento que ocurre cuando cambia la propiedad IdxImg
    static void OnIdxImgChange(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      { 
      var This = obj as CtrlPush;
      var Idx  = (int)args.NewValue;

      if( Idx>=0 && Idx<This.BitMaps.Count )
        This.Source = This.BitMaps[Idx];
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Propiedades relacionadas con el posicinamiento del control en la escena
    public int Col;
    public int Row;
    public CVal On;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Propiesad 
    double angle;
    public double Angle
      {
      get { return angle; }
      set { 
          if( value<   0.0  ) value = 360 - value;                            // Noramaliza los angulos
          if( value>= 360.0 ) value = value - 360;

          var grp    = (TransformGroup  ) this.RenderTransform;               // Grupo de trasformacines asociadas al pusher
          var Rotate = (RotateTransform) grp.Children[0];                     // La primera es la transformación para rotación

          Rotate.BeginAnimation( RotateTransform.AngleProperty, null );       // Libera la propiedad si se estaba animando

          Rotate.CenterX = Escena.zCell/2;                                    // Pone centros de rotación en el cento de la celda
          Rotate.CenterY = Escena.zCell/2;

          Rotate.Angle = value;                                               // Pone el angulo nuevo
          angle = value;                                           
          }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Definición de la lista de imagenes asociadas al control
    public List<BitmapImage> BitMaps = new List<BitmapImage>();
    string _NameBase = null;

    public string NameBase  { 
                            get { return _NameBase; } 
                            set {
                                _NameBase = value;
                                if( value != null ) LoadBitmaps();
                                } 
                            }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Carga todas las imagenes para el movimiento del pusher, según el nombre base (siempre la imagen principal es la primera)
    private void LoadBitmaps()
      {
      var bmp = Escena.CreateBitmap( _NameBase );
      Source = bmp;

      BitMaps.Clear();
      if( bmp!=null ) BitMaps.Add( bmp );

      for( int i=1; i<40; ++i )
        {
        var fName = _NameBase.Replace( ".png", i + ".png" );
        bmp = Escena.CreateBitmap( fName );
        if( bmp==null ) break;

        BitMaps.Add( bmp );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void Move(int col, int row, bool SetOn=true )
      {
      if( SetOn )
        Escena.SetCell( Col, Row, On );

      Col = col;
      Row = row;
      On  = Escena.GetCell( col, row );

      Escena.SetCell( col, row, Cell.Pusher );

      Canvas.SetLeft( this, Escena.GetX( col ) ); 
      Canvas.SetTop ( this, Escena.GetY( row ) ); 
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void UpdatePos()
      {
      Canvas.SetLeft( this, Escena.GetX( Col ) ); 
      Canvas.SetTop ( this, Escena.GetY( Row ) ); 
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Pone los datos del pusher en forma de cadena
    public override string ToString()
      {
 	    return  Col.ToString() + ',' + Row + ',' + Path.GetFileName( _NameBase ) + ',' + angle;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene los datos del pusher desde una cadena en el formato col,row,namebase
    public static bool FromString( string sData )
      { 
      var tokens = sData.Split(',');
      if( tokens.Length < 3 ) return false;

      try 
	      {	        
		    int col = int.Parse( tokens[0] );
		    int row = int.Parse( tokens[1] );

        Escena.Pusher.Move( col, row, false );
        Escena.Pusher.NameBase = Escena.GetEscenaPath( tokens[2] );

        if( tokens.Length >= 4 )
          {
          double Ang;
          if( double.TryParse( tokens[3], out Ang ) )
            Escena.Pusher.Angle = Ang;
          }
        else
          Escena.Pusher.Angle = 0.0;

        return true;
	      }
	    catch {  return false; }
      }

    }//============================================================================================================================================================
  }
