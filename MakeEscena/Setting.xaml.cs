using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  /// Maneja el recuadro donde se definen las caracteristicas de la escena
  public partial class Setting : UserControl
    {
    public event EventHandler EscenaChanged;

    TextChangedEventHandler HandleChangeRegilla    = null;
    TextChangedEventHandler HandleChangeCellSize   = null;
    TextChangedEventHandler HandleChangeResolution = null;

    //public Canvas GameZone { get; set; }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public Setting()
      {
      InitializeComponent();

      HandleChangeRegilla    = new TextChangedEventHandler( OnChangeRegilla );
      HandleChangeCellSize   = new TextChangedEventHandler( OnChangeCellSize );
      HandleChangeResolution = new TextChangedEventHandler( OnChangeResolution );

      Panel.SetZIndex( this, 2000 );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void Show( bool show = true )
      {
      if( show )
        { 
        UpdateInfo();
        Visibility = Visibility.Visible;
        }
      else
        {
        Visibility = Visibility.Hidden;

        txtRows.TextChanged -= HandleChangeRegilla;
        txtCols.TextChanged -= HandleChangeRegilla;
        txtCels.TextChanged -= HandleChangeCellSize;
        txtResH.TextChanged -= HandleChangeResolution;
        txtResW.TextChanged -= HandleChangeResolution;
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnChangeCellSize(object sender, TextChangedEventArgs e)
      {
      if( Escena.GameZone == null ) return;

      int cSize=0;

      if( !int.TryParse( txtCels.Text, out cSize )  ) return;
      if( cSize<10 || cSize>150 ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      int nRows = Escena.Rows;
      int nCols = Escena.Cols;
 
      if( chkAuto.IsChecked==true )
        { 
        nRows = (int)( Escena.GameZone.ActualHeight+0.1 )/cSize;
        nCols = (int)( Escena.GameZone.ActualWidth +0.1 )/cSize;
        }

      Escena.Init( cSize, nCols, nRows );

      UpdateInfo();

      if( EscenaChanged != null )
        EscenaChanged( this, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnChangeRegilla(object sender, TextChangedEventArgs e)
      {
      if( Escena.GameZone == null ) return;

      int rows=0, cols=0;

      if( !int.TryParse( txtRows.Text, out rows )  ) return;
      if( !int.TryParse( txtCols.Text, out cols )  ) return;
      if( rows<5 || rows>50 || cols<5 || cols>50   ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      var cSize  = Escena.zCell;
      if( chkAuto.IsChecked==true )
        { 
        var cYSize = ( Escena.GameZone.ActualHeight+0.1 )/rows;
        var cXSize = ( Escena.GameZone.ActualWidth +0.1 )/cols;
        cSize  = (int)(( cXSize < cYSize )? cXSize : cYSize);
        }

      Escena.Init( cSize, cols, rows );

      UpdateInfo();

      if( EscenaChanged != null )
        EscenaChanged( this, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnChangeResolution(object sender, TextChangedEventArgs e)
      {
      int XRes=0, YRes=0;

      if( !int.TryParse( txtResW.Text, out XRes )  ) return;
      if( !int.TryParse( txtResH.Text, out YRes )  ) return;

      if( XRes<=200 || YRes<=200 || XRes>2500 || XRes>2500  ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      if( Escena.GameZone != null )
        { 
        Escena.GameZone.Width  = XRes;
        Escena.GameZone.Height = YRes;
        }

      if( chkAuto.IsChecked==true )
        Escena.Init( Escena.zCell, XRes/Escena.zCell, YRes/Escena.zCell );

      UpdateInfo();

      if( EscenaChanged != null )
        EscenaChanged( this, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void UpdateInfo()
      {
      txtRows.TextChanged -= HandleChangeRegilla;
      txtCols.TextChanged -= HandleChangeRegilla;
      txtCels.TextChanged -= HandleChangeCellSize;
      txtResH.TextChanged -= HandleChangeResolution;
      txtResW.TextChanged -= HandleChangeResolution;

      txtCels.Text = Escena.zCell.ToString();

      txtCols.Text = Escena.Cols.ToString();
      txtRows.Text = Escena.Rows.ToString();

      txtOffX.Text = Escena.OffSetX.ToString();
      txtOffY.Text = Escena.OffSetY.ToString();

      txtAreaW.Text = Escena.Width.ToString();
      txtAreaH.Text = Escena.Height.ToString();

      if( Escena.GameZone != null )
        { 
        txtResW.Text = Escena.GameZone.Width.ToString();
        txtResH.Text = Escena.GameZone.Height.ToString();
        }

      txtRows.TextChanged += HandleChangeRegilla;
      txtCols.TextChanged += HandleChangeRegilla;
      txtCels.TextChanged += HandleChangeCellSize;
      txtResH.TextChanged += HandleChangeResolution;
      txtResW.TextChanged += HandleChangeResolution;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Limpia la escena para diseñar una escena nueva
    private void OnNewEscena(object sender, RoutedEventArgs e)
      {
      if( !AnimatePath.InvalideSolution() ) return;

      Escena.Init( Escena.zCell, Escena.Cols, Escena.Rows );

      if( EscenaChanged != null )
        EscenaChanged( this, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Recorta la escena, quitando las ultimas filas y columnas que solo tienen pared
    private void OnRecortaEscena(object sender, RoutedEventArgs e)
      {
      if( Escena.Recorta() )
        { 
        AnimatePath.InvalideSolution();

        if( EscenaChanged != null )
          EscenaChanged( null, new EventArgs() );

        UpdateInfo();
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Adiciona una columna a la escena
    private void OnAddColumn(object sender, RoutedEventArgs e)
      {
      int col;
      if( int.TryParse( txtColToAdd.Text, out col ) )
        {
        Escena.AddCol( col );
        AnimatePath.InvalideSolution();
        }

      UpdateInfo();
      if( EscenaChanged != null )
        EscenaChanged( null, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Adiciona una fila a la escena
    private void OnAddRow(object sender, RoutedEventArgs e)
      {
      int row;
      if( int.TryParse( txtRowToAdd.Text, out row ) )
        {
        Escena.AddRow( row );
        AnimatePath.InvalideSolution();
        }

      UpdateInfo();
      if( EscenaChanged != null )
        EscenaChanged( null, new EventArgs() );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra una columna de la escena
    private void OnDelColumn(object sender, RoutedEventArgs e)
      {
      int col;
      if( int.TryParse( txtColToAdd.Text, out col ) )
        {
        if( Escena.DelCol( col ) )
          { 
          AnimatePath.InvalideSolution();

          UpdateInfo();
          if( EscenaChanged != null )
            EscenaChanged( null, new EventArgs() );
          }
        else
          MessageBox.Show("No se pudo borrar la columna porque no esta vacia" );
        }

      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra una fila de la escena
    private void OnDelRow(object sender, RoutedEventArgs e)
      {
      int row;
      if( int.TryParse( txtRowToAdd.Text, out row ) )
        {
        if( Escena.DelRow( row ) )
          { 
          AnimatePath.InvalideSolution();

          UpdateInfo();
          if( EscenaChanged != null )
            EscenaChanged( null, new EventArgs() );
          }
        else
          MessageBox.Show("No se pudo borrar la fila porque no esta vacia" );
        }
      }

    }//============================================================================================================================================================
  }
