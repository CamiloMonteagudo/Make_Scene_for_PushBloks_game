using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  // Modos de trabajo en la edicción de la zona de juego
  public enum Mode
    {
    Cursor,
    Pared,
    Bloque,
    Target,
    ReSize,
    Play,
    }

  //==============================================================================================================================================================
  /// Interaction logic for MainWindow.xaml
  public partial class MainWindow : Window
    {
    static public List<CtrlTarget> Targets = new List<CtrlTarget>();      // Lista de objetos donde se deben poner los bloques (Solo se usan en diseño)

    Mode Modo = Mode.Cursor;

    TextChangedEventHandler HandleChangeBlockInfo;          // Evento para el cambio de la información de un bloque o target
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public MainWindow()
      {
      InitializeComponent();

      chkPared.IsChecked  = Escena.ShowPared;
      chkGrid.IsChecked   = Escena.ShowGrid;
      chkTarget.IsChecked = Escena.ShowTarget;
      chkID.IsChecked     = Escena.ShowID;
      chkFondo.IsChecked  = Escena.ShowFondo;

      HandleChangeBlockInfo = new TextChangedEventHandler(this.OnChangeBlockInfo);  // Inicializa evento para cambio de información

      PusherPath.Init( pPath );                             // Inicializa los paramentrios para definir el camino del pusher
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnLoaded(object sender, RoutedEventArgs e)
      {
      Escena.Init( 80, 8, 12, GameZone );                   // Inicializa la escena con los valores por defecto
      
      DrawGrid();                                           // Dibuja la regilla
      Escena.SetPusher( Pusher );                           // Asocia el pusher a la escena
      SetMode( Mode.Cursor );                               // Pone el modo de trabajo por defecto

      AnimatePath.EndAnimatedMove = EndAnimatedMove;        // Pone función para saber cuando terminan las animaciones
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Maneja el estado de loa botones de la parte de arriba y el establece el modo de trabajo
    private void SetMode( Mode mode, bool sw = false )
      {
      CtrlBtnMenu ctrl;
      CellData.Visibility = Visibility.Hidden;              // Esconde la información sobre la celda actual

      switch( mode )                                        // De acuerdo al modo de trabajo, determina el boton actual
	      {
		    case Mode.Cursor: ctrl = btnMover;  break;
        case Mode.Pared : ctrl = btnPared;  break;
        case Mode.Bloque: ctrl = btnBlock;  break;
        case Mode.Target: ctrl = btnTarget; break;
        case Mode.ReSize: ctrl = btnReSize; break;
        case Mode.Play  : Modo = mode;      return;
        default: return;
	      }

      var chk = ctrl.Check;                                 // Guarda estado actual del boton actual

      btnMover.Check  = false;                              // Pone todos los botones sin señalar
      btnPared.Check  = false; 
      btnBlock.Check  = false; 
      btnTarget.Check = false;
      btnReSize.Check = false;

      Modo = mode;                                          // Establce el modo de trabajo

      if( sw && chk )                                       // Si el boton esta señalado y se llamo con opción de cambio de estado
        {
        ctrl.Check = false;                                 // Deseñala el botón

        Modo = Mode.Cursor;                                  // Pone el modo Mover que esl modo por defecto
        btnMover.Check  = true;                             // Lo señala
        }
      else                                                  // En otro caso
        ctrl.Check = true;                                  // Siempre señala el boton actual

      EscenaDef.Show( Modo == Mode.ReSize );                // Si el modo de definición de la escena, muestra panel de datos
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateBlock( int col, int row )
      {
      var Val = Escena.GetCell(col,row);
      if( Val.Tipo == Cell.Bloque ) DeleteBlock( col, row );
      if( !Val.IsPiso() ) return; 

      int idx = Escena.Blocks.Count;
      if( idx>15 ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      var bck = Escena.AddBlock( col, row, 0 );
      GameZone.Children.Add( bck.Ctrl );

      SetBlocksInfo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DeleteBlock(int col, int row)
      {
      if( !AnimatePath.InvalideSolution() ) return;

      var bck = Escena.DelBlock( col, row );

      GameZone.Children.Remove( bck.Ctrl );

      SetBlocksInfo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void SetBlocksInfo()
      {
      int Bcks = Escena.Blocks.Count;
      int Trgs = Targets.Count;

      txtBcks.Text = "B=" + Bcks + " T=" + Trgs;
      }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static public int FindTarget( int col, int row )
      {
      for( int i=0; i<Targets.Count; ++i )
        {
        var trg = Targets[i];
        if( trg.xCell == col && trg.yCell == row ) return i;
        }

      return -1;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateTarget( int col, int row )
      {
      var Val = Escena.GetCell( col, row );

      if( Val.Tipo == Cell.Target ) DeleteTarget( col, row );
      if( Val.Tipo != Cell.Piso    ) return;        

      int idx = Targets.Count;
      if( idx>15 ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      Escena.SetCell( col, row, Cell.Target );
      CreateTargetObj( col, row );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateTargetObj( int col, int row )
      {
      var Val = Escena.GetCell( col, row );
      if( Val.Tipo != Cell.Target ) return;

      var tg = new  CtrlTarget( col, row, Val.Info );
      GameZone.Children.Add( tg );
      Targets.Add( tg );

      SetBlocksInfo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DeleteTarget(int col, int row)
      {
      int idx = FindTarget( col, row );
      if( idx<0 ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      var tg = Targets[idx];

      Targets.RemoveAt(idx);
      GameZone.Children.Remove( tg );
      Escena.SetCell( col, row, Cell.Piso );

      SetBlocksInfo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnShowDimentions(object sender, MouseButtonEventArgs e)
      {
      e.Handled = true;

      SetMode( Mode.ReSize, true );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DrawGrid()
      {
      DeleteObj( DrawTipo.Grid );

      if( Escena.Cols<=0 || Escena.Rows<=0 || !Escena.ShowGrid ) return;

      var w     = Escena.Width;
      var h     = Escena.Height;
      var cSize = Escena.zCell;
      var xIni  = Escena.OffSetX;
      var yIni  = Escena.OffSetY;

      var Col = new SolidColorBrush( Colors.Black );

      for( double y=yIni; y<=(yIni+h); y += cSize )
        { 
        var ln = new Line{ X1 = xIni, X2 = xIni+w,
                           Y1 = y,    Y2 = y,
                           Tag = DrawTipo.Grid,
                           Stroke = Col, 
                           StrokeThickness = 0.5, 
                           StrokeDashArray = new DoubleCollection{ 10.0, 10.0 } }; 
 
        GameZone.Children.Add( ln );
        }


      for( double x=xIni; x<=(xIni+w); x += cSize )
        { 
        var ln = new Line{ X1 = x,    X2 = x,
                           Y1 = yIni, Y2 = yIni+h,
                           Tag = DrawTipo.Grid,
                           Stroke = Col, 
                           StrokeThickness = 0.5,
                           StrokeDashArray = new DoubleCollection{ 10.0, 10.0 } }; 

        
        GameZone.Children.Add( ln );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DeleteObj( DrawTipo tipo )
      {
      int n = GameZone.Children.Count;

      int nDel = 0;
      for( int i=0; i<n; i++ )
        {
        var Elem = (FrameworkElement)GameZone.Children[ i-nDel ];
        var Tag = Elem.Tag;
        if( Tag!=null && (((DrawTipo)Tag)& tipo) != 0  )
          {
          GameZone.Children.Remove( Elem);

          ++nDel;
          }
        }

      if( (tipo & DrawTipo.Block ) != 0 )
        { 
        Escena.ClearBlocks();
        SetBlocksInfo();
        }

      if( (tipo & DrawTipo.Target) != 0 ) 
        {
        Targets.Clear();
        SetBlocksInfo();
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private Cell ShowCellInfo( int col, int row )
      {
      var  Val = Escena.GetCell(col,row);
      var tipo = Val.Tipo;
      if( Modo != Mode.Cursor ) return tipo;

      CellData.Visibility = Visibility.Visible;

      txtInfo.TextChanged -= HandleChangeBlockInfo;

      txtCell.Text = "(" + col + "," + row + ")";
      txtImg.Text  =  Escena.GetName( Escena.sImgFondo );

      CellInfo.Visibility = Visibility.Hidden;
      btnRotar.Visibility = Visibility.Hidden;
      btnTime.Visibility  = Visibility.Hidden;
      selFondo.Visibility =  Visibility.Hidden;

      switch (tipo)
	      {
        case Cell.Piso  :
            { 
            txtTipo.Text = "Piso" ;
            selFondo.Visibility =  Visibility.Visible;
            break;
            }
        case Cell.Pusher:
            { 
            txtTipo.Text = "Pusher"; 
            txtImg.Text  =  Escena.GetName( Escena.Pusher.NameBase );
            btnRotar.Visibility = Visibility.Visible;
            btnTime.Visibility  = Visibility.Visible;
            break;
            }
        case Cell.Pared :
            { 
            txtTipo.Text = "Pared";
            selFondo.Visibility =  Visibility.Visible;
            break;
            }
        case Cell.Bloque: 
            {
            var idx      = Val.Info;
            var bck      = Escena.Blocks[idx];
            LastTipo     = Cell.Bloque;
            txtTipo.Text = "Bloque";      
            txtIdx.Text  = idx.ToString();  
            txtInfo.Text = bck.ID.ToString();
            txtImg.Text  = Escena.GetName( bck.Image );

            CellInfo.Visibility = Visibility.Visible;
            txtInfo.SelectAll();
            break;
            }
        case Cell.Target: 
            {
            var idx      = FindTarget( col, row );
            LastTipo     = Cell.Target;
            txtTipo.Text = "Target";      
            txtIdx.Text  = idx.ToString();  

            if( idx >= 0)
              txtInfo.Text = Targets[idx].ID.ToString();

            CellInfo.Visibility = Visibility.Visible;
            txtInfo.SelectAll();
            break;
            }
		    default  : txtTipo.Text = "Desconocida"; break;
	      }

      txtInfo.TextChanged += HandleChangeBlockInfo;
      return tipo;
      }
    
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void CreatePared(int col, int row)
      {
      if( col>=Escena.Cols || row>=Escena.Rows ) return;

      if( Escena.GetCell(col,row).Tipo != Cell.Piso ) return;               // Si no es piso no puede poner una pared

      if( !AnimatePath.InvalideSolution() ) return;

      if( Escena.ShowPared == false )  
        {
        chkPared.IsChecked = true;
        OnClickShowPared( null, null );
        }

      Escena.SetCell( col, row, Cell.Pared );

      CreateParedObj( col, row );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateParedObj( int col, int row )
      {
      if( !Escena.ShowPared ) return;

      var rc  = new Border{   Background = new SolidColorBrush( Colors.Gray ),
                              Width      = Escena.zCell,
                              Height     = Escena.zCell,
                              Tag        = DrawTipo.Pared };

      Canvas.SetTop ( rc, Escena.GetY(row) );
      Canvas.SetLeft( rc, Escena.GetX(col) );
      Canvas.SetZIndex( rc, 100 ); 

      GameZone.Children.Add( rc );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void DeletePared(int col, int row)
      {
      if( Escena.GetCell(col,row).Tipo != Cell.Pared ) return;

      if( !AnimatePath.InvalideSolution() ) return;

      if( Escena.ShowPared == false )  
        {
        chkPared.IsChecked = true;
        OnClickShowPared( null, null );
        }

      Escena.SetCell( col, row, Cell.Piso );

      var x1 = Escena.GetX(col);
      var y1 = Escena.GetY(row);

      int nDel = 0;
      int n    = GameZone.Children.Count;
      for (int i = 0; i<n; i++ )
        {
        var Elem = (FrameworkElement)GameZone.Children[i-nDel];
        var Tag = Elem.Tag;
        if (Tag == null || (((DrawTipo)Tag) & DrawTipo.Pared) == 0 )
          continue;

        var x2 = Canvas.GetLeft( Elem );
        var y2 = Canvas.GetTop ( Elem );
        if( x1==x2 && y1==y2 )
          { 
          GameZone.Children.Remove( Elem );
          ++nDel;
          }
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnDrawPared(object sender, MouseButtonEventArgs e)
      {
      SetMode( Mode.Pared, true );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static int LastCol = -1;
    static int LastRow = -1;

    UIElement LastObj  = null;
    Cell      LastTipo = 0;
    Timer    PalyTimer = null;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
      {
      e.Handled = true;
      LastObj = null;

      if( AnimObj!=null && AnimObj.InCurse() ) return;

      if( Modo == Mode.ReSize ) SetMode( Mode.Cursor );

      var pnt = e.GetPosition( GameZone );

      LastCol = Escena.GetCol(pnt.X);
      LastRow = Escena.GetRow(pnt.Y);

      if( LastCol<0 || LastRow<0 | LastCol>=Escena.Cols | LastRow>=Escena.Rows ) return;

      LastTipo = ShowCellInfo( LastCol, LastRow );

           if( Modo == Mode.Pared  ) CreatePared ( LastCol, LastRow );
      else if( Modo == Mode.Bloque ) CreateBlock ( LastCol, LastRow );
      else if( Modo == Mode.Target ) CreateTarget( LastCol, LastRow );
      else if( Modo == Mode.Cursor  )
        { 
        var Val = Escena.GetCell(LastCol,LastRow);

        if( LastTipo == Cell.Bloque ) LastObj = Escena.Blocks [Val.Info].Ctrl;  
        if( LastTipo == Cell.Target ) { 
                                      var idx = FindTarget( LastCol, LastRow ); 
                                      if( idx>=0 ) LastObj = Targets[idx];  
                                      }    
        if( LastTipo == Cell.Pusher ) LastObj = Escena.Pusher;  
        }
      else if( Modo == Mode.Play  )
        { 
        if( PalyTimer == null )
          {
          GameZone.CaptureMouse();
          PusherPath.SetInitPoint( pnt );
          }
        else
          {
          PalyTimer.Close();
          PalyTimer = null;
          PusherPath.ContinuePath( pnt );
          }
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnMouseRightDown(object sender, MouseButtonEventArgs e)
      {
      e.Handled = true;

      if( Modo != Mode.Pared ) return;
      if( !AnimatePath.InvalideSolution() ) return;

      var pnt = e.GetPosition( GameZone );

      int col = Escena.GetCol(pnt.X);
      int row = Escena.GetRow(pnt.Y);

      DeletePared( col, row );

      LastCol = col;
      LastRow = row;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnMouseMove(object sender, MouseEventArgs e)
      {
      e.Handled = true;

      if( AnimObj!=null && AnimObj.InCurse() ) return;

      bool lDown = ( e.LeftButton  == MouseButtonState.Pressed );
      bool rDown = ( e.RightButton == MouseButtonState.Pressed );
      if( !lDown && !rDown ) return;

      var pnt = e.GetPosition( GameZone );

      int col = Escena.GetCol(pnt.X);
      int row = Escena.GetRow(pnt.Y);

      if( Modo == Mode.Play )
        {
        PusherPath.AddPoint( pnt ); 
        }
      else if( Modo == Mode.Pared )
        {
        if( col != LastCol || row != LastRow )
          { 
          if( lDown ) CreatePared( col, row );
          else        DeletePared( col, row );

          LastCol = col;
          LastRow = row;
          }
        }
      else if( Modo == Mode.Cursor && LastObj!=null && lDown )
        {
        Canvas.SetTop ( LastObj, Escena.GetY(row) );
        Canvas.SetLeft( LastObj, Escena.GetX(col) );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
      {
      e.Handled = true;
      if( AnimObj!=null && AnimObj.InCurse() ) return;

      if( Modo == Mode.Cursor && LastObj!=null  ) EndMoveObj();
      if( Modo == Mode.Play )
        {
        PalyTimer = new System.Timers.Timer(1000);
        PalyTimer.Elapsed += new ElapsedEventHandler( OnPlayEvent );

        PalyTimer.AutoReset = false;
        PalyTimer.Enabled   = true;

        GameZone.ReleaseMouseCapture();
        }
      }

    private void OnMouseLeave(object sender, MouseEventArgs e)
      {
      e.Handled = true;
      if( Modo == Mode.Cursor && LastObj!=null  ) EndMoveObj();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    AnimatePath AnimObj = null;           // Objeto para animación, permanece diferente de null durante las animaciones

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se llama cuando se termina de definir un camino para el pucher y comienza la animación del movimiento
    private void OnPlayEvent(object source, ElapsedEventArgs e)
      {
      PalyTimer.Close();                                              // Libera recursos del timer
      PalyTimer = null;                                               // Lo hace null, para indicar que no es esta esperando

      ContAnim = false;
      Dispatcher.Invoke( ()=> {                                                 // Para que se ejecuten en el hilo principal
                              if( AnimObj==null || !AnimObj.InCurse() )        // Si no estamos ejecutando de una animación y hay un camino definido
                                AnimObj = new AnimatePath( PusherPath.Items,
                                                           PusherPath.Count, 
                                                           false           );   // Anima el desplazamientos según el camino definido
                              } );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que indica que termino un movimento en animación del pusher (con o sin bloque)
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public void EndAnimatedMove( int tipo )
      {
      if( tipo==0 ) ContAnim = false;
      AnimObj = null;                                                 // Para notar que no hay ninguna animación en curso  
      PusherPath.Clear();                                             // Borra el camino

      txtMoves.Text = AnimatePath.MovesText();                        // Actualiza cantidad de movimientos realizados

      if( tipo==0                   &&                                // Si es un animación, de juego
          AnimatePath.SetInTraget() && 
          Escena.CheckBlockPos()    )                                 // Si se posiciona sobre un target y todos lo bloques estan posicionado
        {
        AnimatePath.SaveSolution();                                   // Guarda la solución
        txtSolut.Text = AnimatePath.SolutionText();                   // Actualiza la información de la solución

        MessageBox.Show( "La escena esta resuelta" );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Completa el movimiento de un objeto en modo diseño
    private void EndMoveObj()
      {
      int col = Escena.GetCol( Canvas.GetLeft(LastObj) );             // Localización del objeto que se esta moviendo
      int row = Escena.GetRow( Canvas.GetTop (LastObj) );

      var newVal = Escena.GetCell(col,row);                           // Contenido de la celda sobre la que esta el objeto
      if( newVal.Tipo != Cell.Piso )                                  // Si no es piso, determina si se puede sobreponer
        {
        bool overlap = false;
        if( LastTipo == Cell.Bloque && newVal.IsPiso() ) overlap = true;    // Un bloque sobre un elemento del grupo piso (Target)
        if( LastTipo == Cell.Pusher && newVal.IsPiso() ) overlap = true;    // El pusher sobre un elemento del grupo piso (Target)

        if( !overlap )                                                // Si no se puede sobreponer
          { 
          Canvas.SetTop ( LastObj, Escena.GetY(LastRow) );            // Regresa el objeto a su posición original
          Canvas.SetLeft( LastObj, Escena.GetX(LastCol) );

          LastObj = null;                                             // Anula objeto que se esta moviendo
          return;                                                     // No hace más nada
          }
        }

      if( !AnimatePath.InvalideSolution() && LastObj != null )
        { 
        Canvas.SetTop ( LastObj, Escena.GetY(LastRow) );            // Regresa el objeto a su posición original
        Canvas.SetLeft( LastObj, Escena.GetX(LastCol) );

        LastObj = null;                                             // Anula objeto que se esta moviendo
        return;                                                     // No hace más nada
        }

      var Val = Escena.GetCell(LastCol,LastRow);                      // Contenido de la celda donde estaba el objeto

      if( LastTipo == Cell.Bloque )                                   // Si el objeto que se esta moviendo es del tipo bloque
        {
        var blq  = Escena.Blocks[ Val.Info ];                         // Obtiene el bloque a partir de información de la celda

        Escena.SetCell( LastCol, LastRow, blq.On );                   // Pone contenido que habia anteriormente en la celda donde estaba el bloque 

        blq.On  = newVal;                                             // Guarda el contenido de la celda, sobre la que va a poner
        blq.Col = col;                                                // Actualiza fila y columna de donde esta
        blq.Row = row;
        }

      if( LastTipo == Cell.Target )                                   // Si el objeto que se esta moviendo es un target
        {
        int idx = FindTarget( LastCol, LastRow );                     // Encuentra el target según la pocisión donde estaba
        if( idx>=0 )                                                  // Si lo encuentra
          { 
          var tg   = Targets[ idx ];  
          tg.xCell = col;                                             // Actualiza al fila y columna donde esta
          tg.yCell = row;
          }

        Escena.SetCell( LastCol, LastRow, Cell.Piso );                // Pone contenido de la celda donde estaba como piso
        }

      if( LastTipo == Cell.Pusher )                                   // Si el objeto que se esta moviendo es el pusher
        {
        Escena.MovePusher( col, row );                                // Lo mueve a la nueva posición
        }

      Escena.SetCell( col, row, Val );                                // Actualiza contenid de la celda hacia la que movio el objeto

      LastObj = null;                                                 // Anula el movimento del objeto
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton bloque en la parte superior
    private void OnAddBloque(object sender, MouseButtonEventArgs e)
      {
      SetMode( Mode.Bloque, true );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton target en la parte superior
    private void OnAddTarget(object sender, MouseButtonEventArgs e)
      {
      SetMode( Mode.Target, true );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton cursor en la parte superior
    private void OnMover(object sender, MouseButtonEventArgs e)
      {
      SetMode( Mode.Cursor, true );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando es edita el la información del los bloques o targets de la parte inferior
    private void OnChangeBlockInfo(object sender, TextChangedEventArgs e)
      {
      if( LastCol<0 || LastRow<0 | LastCol>=Escena.Cols | LastRow>=Escena.Rows ) return;

      int Info = -1;

      if( !int.TryParse( txtInfo.Text, out Info )  ) return;
      if( Info<0 || Info>15                        ) return;

      var Val = Escena.GetCell(LastCol,LastRow);

      AnimatePath.InvalideSolution();

      if( LastTipo == Cell.Bloque  )
        { 
        var bck   = Escena.Blocks[ Val.Info ];
        bck.Image = Escena.FindImage( Info );
        bck.ID    = Info;
        }

      if( LastTipo == Cell.Target  ) 
        {
        int idx = FindTarget( LastCol, LastRow );

        if( idx>=0 )  
          {
          Targets[idx].ID = Info;
          Escena.SetCell( LastCol, LastRow, new CVal( Cell.Target, Info ) );
          }
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cada vez que se cambia la configuración de la escena, mediante el boton dimesiones y Setting
    private void OnEscenaChanged(object sender, EventArgs e)
      {
      if( sender!= null )
        { 
        DeleteObj( DrawTipo.All );                                                // Borra todos los objetos

        Escena.sImgFondo = null;                                                  // Quita nombre de imagen de fondo de la escena
        ImgFondo.Source  = null;                                                  // Quita imagen de fondo de la pantalla
        }
      else
        { 
        DeleteObj( DrawTipo.Pared| DrawTipo.Grid | DrawTipo.Target );             // Borra todos los objetos

        UpdateObjsFromEscena();                                                   // Crea los objetos Pared y Target con información de la escena
        for( int i=0; i<Escena.Blocks.Count; i++ )                                // Recorre todos los bloques que hay en la escena
          Escena.Blocks[i].UpdatePos() ;
  
        Escena.Pusher.UpdatePos();
        }

      ScaleGameZona();

      DrawGrid();                                                               // Redibuja la regilla si esta activo
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Escala la zona de juego para que se pueda acomodar la escena completa
    private void ScaleGameZona()
      {
      var xScale = this.ActualWidth        / GameZone.Width;
      var yScale = CentralRow.ActualHeight / GameZone.Height;

      double scale = 1.0;
      if( xScale<1 || yScale<1 )
        scale = (xScale < yScale)? xScale : yScale;

      Zoom.ScaleX = scale;
      Zoom.ScaleY = scale;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton de imagen de la parte inferior
    private void OnClickImagen(object sender, RoutedEventArgs e)
      {
      if( LastCol<0 || LastRow<0 | LastCol>=Escena.Cols | LastRow>=Escena.Rows ) return;

      var dlg = new OpenFileDialog();

      dlg.Title  = "Selecionar el archivo de imagen"; 
      dlg.Filter = "Archivo de imagen (.png,.jpg)|*.png;*.jpg"; 

      if( dlg.ShowDialog() != true ) return;
      
      string ImgFName = dlg.FileName;

      var Val = Escena.GetCell(LastCol,LastRow);

      if( LastTipo == Cell.Bloque )
        {
        var Id = Escena.Blocks[ Val.Info ].ID;
        for( int i=0; i<Escena.Blocks.Count; ++i )
          {
          var bck = Escena.Blocks[i];
          if( bck.ID == Id ) bck.Image = ImgFName;
          }
        }
      else if( LastTipo == Cell.Pusher )
        {
        Escena.Pusher.NameBase = ImgFName;
        }
      else 
        {
        Escena.sImgFondo = ImgFName;
        chkFondo.IsChecked = true;
        OnClickShowFondo( null, null );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se toca el checkbock de 'Regilla' de la parte inferior derecha (Mostar/Ocultar regilla)
    private void OnClickShowGrid(object sender, RoutedEventArgs e)
      {
      Escena.ShowGrid = ( chkGrid.IsChecked == true );                          // Pone bandera de mostrar Regilla según estado del checkbox
      DrawGrid();                                                               // Muestra/Oculta regilla según el estado de la bandera
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se toca el checkbock 'Target' de la parte inferior derecha (Mostrar/Ocultar Targets)
    private void OnClickShowTarget(object sender, RoutedEventArgs e)
      {
      Escena.ShowTarget = ( chkTarget.IsChecked == true );                      // Pone bandera de mostrar Target según estado del checkbox

      foreach( var Trg in Targets )                                             // Recorre todos los objetos target
        Trg.Show();                                                             // Muestra/Oculta el objeto según el estado de la bandera
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se toca el checkbock 'ID' de la parte inferior derecha  (Muestra/Oculta Id de los objetos)
    private void OnClickShowID(object sender, RoutedEventArgs e)
      {
      Escena.ShowID = ( chkID.IsChecked == true );                              // Pone bandera de mostrar ID según estado del checkbox

      foreach( var Trg in Targets )                                             // Recorre todos los objetos Targets actualizando estado del ID
        Trg.ShowId();

      foreach( var Blk in Escena.Blocks )                                       // Recorre todos los objetos Bloques actualizando estado del ID
        Blk.Ctrl.ShowId();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se toca el checkbock 'Pared' de la parte inferior derecha (Muestra/Oculta objetos pared)
    private void OnClickShowPared(object sender, RoutedEventArgs e)
      {
      Escena.ShowPared = ( chkPared.IsChecked == true );                        // Pone bandera de mostrar pared según estado del checkbox

      if( !Escena.ShowPared )                                                   // Si no hay que mostrar pared
        DeleteObj( DrawTipo.Pared );                                            // Borra todos lo objetos del tipo pared en pantalla
      else                                                                      // Hay que mostrar objetos pared
        { 
        for( int row=0; row<Escena.Rows; ++row )                                // Crea todos los objetos pared de acuerdo al contenido de las celdas
          for( int col=0; col<Escena.Cols; ++col )
            if( Escena.GetCell(col, row).Tipo == Cell.Pared )
              CreateParedObj( col, row );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnClickShowFondo(object sender, RoutedEventArgs e)
      {
      Escena.ShowFondo = ( chkFondo.IsChecked == true );                        // Pone bandera de mostrar pared según estado del checkbox

      if( Escena.ShowFondo )                                                    // Si hay que mostrar el fondo
        { 
        var ImgPath = Escena.GetEscenaPath( Escena.sImgFondo, false );          // Obtiene el nombre de la imagen de fondo
        ImgFondo.Source = Escena.CreateBitmap( ImgPath );                       // La carga y la muestra en pantalla
        }
      else
        ImgFondo.Source = null;                                                 // Quita la imagen del fondo
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Rota la posición inicial del pusher
    private void OnRotatePusher(object sender, RoutedEventArgs e)
      {
      Pusher.Angle +=  90.0;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cambia el tiempo de la secuencia de animación del pusher
    private void OnAnimTime(object sender, RoutedEventArgs e)
      {
      var t = AnimatePath.AmimTime - 0.05;
      if( t<0 ) t = 0.5;

      AnimatePath.AmimTime = t;
      SetBtnTime();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Pone el tiempo actual en el boton de tiempo
    private void SetBtnTime()
      {
      btnTime.Content = "Time " + (int)(AnimatePath.AmimTime * 100);
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton 'Guardar' de la parte superior
    private void OnSaveEscena(object sender, MouseButtonEventArgs e)
      {
      EscenaDef.Show( false );                                                  // Oculta panel de definicion de escena si esta abierto

      if( AnimObj!=null && AnimObj.InCurse() ) return;                          // Si es esta en medio de una animación no hace nada

      var dlg = new SaveFileDialog();                                           // Crea objeto para seleción del archivo de salva

      dlg.Title      = "Selecionar donde guardar la Escena";                    // Pone titulo del dialogo
      dlg.DefaultExt = ".scene";                                                // Pone extensión por defecto de la escena
      dlg.FileName   = Escena.LastFilePath;
      dlg.Filter     = "Archivo de Escena (.scene)|*.scene";                    // Pone filto para solo mostrar ficheros de escenas

      if( dlg.ShowDialog() != true ) return;                                    // Muestra dialogo de selección

      if( Modo == Mode.Play )                                                   // Si esta en modo jugar
        LoadEscenaFromXml( SnapShot );                                          // Restaura la escena al momento que se empezo a jugar
      else
        SetMode( Mode.Cursor );                                                 // Pone el modo cursor como modo por defecto

      try
        {
        Escena.LastFilePath = dlg.FileName;                                     // Guarda nombre de la ultima escena

        var sXml = Escena.ToXml( false );                                       // Pone todo los datos de la escena en formato XML

        File.WriteAllText( dlg.FileName, sXml );                                // Escribe el fichero con los datos

        Escena.CopyImgs();                                                      // Copia todas las imagenes que se usan en al escena al directorio

        AnimatePath.SolutionChanged = false;                                    // Pone bandera para indicar que la solución ya se guardo         
        }
      catch
        {
        MessageBox.Show( "No se pudo guardar la Escena correctamente" );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Evento que ocurre cuando se oprime el boton 'Cargar' de la parte superior
    private void OnLoadEscena(object sender, MouseButtonEventArgs e)
      {
      EscenaDef.Show( false );                                                  // Oculta panel de definicion de escena si esta abierto

      SetMode( Mode.Cursor );                                                   // Pone modo cursor para después de cargar la escena

      var dlg = new OpenFileDialog();                                           // Crea objeto para seleccion del fichero de escena

      dlg.FileName   = Escena.LastFilePath;                                     // Para que salga por defecto la ultima escena que se cargo
      dlg.Title      = "Selecionar el archivo de escena para cargar";           // Titulo del dialogo
      dlg.DefaultExt = ".scene";                                                // Extensión que se toma por defecto para el fichero
      dlg.Filter     = "Archivo de Escena (.scene)|*.scene";                    // Filtro para que solo muestre archivos de escenas

      if( dlg.ShowDialog() != true ) return;                                    // Muestra el dialogo

      Escena.LastFilePath = dlg.FileName;                                       // Guarda nombre del fichero en la escena
      var sXml = File.ReadAllText( dlg.FileName );                              // Carga el texto completo

      if( !LoadEscenaFromXml( sXml ) )                                          // Carga la escena si el texto es un XML valido
        MessageBox.Show( "No se puedo leer la Escena correctamente" );
      else                                                                      // Si se cargo la escena
        {
        this.Title = dlg.FileName;
        bool vis = (Escena.sImgFondo == null);

        chkPared.IsChecked = vis;                                           // Quita todos los elementos de ayuda al diseño
        OnClickShowPared( null, null );

        chkGrid.IsChecked = vis;
        OnClickShowGrid( null, null );

        chkTarget.IsChecked = vis;
        OnClickShowTarget( null, null );

        chkID.IsChecked = vis;
        OnClickShowID( null, null );

        chkFondo.IsChecked = true;
        OnClickShowFondo( null, null );

        if( AnimatePath.Solution != null )                                      // Tiene una solución ya establecida
          SetGameMode();                                                        // Pasa directamente al modo jugar (En modo diseño se pierda la solución)
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Carga la escena desde una cadena XML
    private bool LoadEscenaFromXml( string sXml )
      {
      DeleteObj( DrawTipo.All );                                                // Borara todos los objetos (Pared, Regilla, Targets y bloques)    

      if( !Escena.FromXml( sXml ) ) return false;                               // Carga todo desde XML

      chkFondo.IsChecked = true;
      UpdateObjsFromEscena();                                                   // Crea los objetos Pared y Target con información de la escena
      UpdateBlocksInfo();                                                       // Actualiza información de los bloques, con el indice en la lista

      DrawGrid();                                                               // Crea y muestar la regilla (Si esta visible)

      AnimatePath.ClearUndo();                                                  // Borra la información de undo
      txtMoves.Text = AnimatePath.MovesText();                                  // Actualiza cantidad de movimientos realizados

      for(int i=0; i<selFondo.Items.Count; ++i )                                // Actualiza al combobox de tipo de relleno de la escena
        {
        var txt = ((string)((ComboBoxItem)selFondo.Items[i]).Content) + ".jpg";
        if( txt==Escena.sFillFondo )
          {
          selFondo.SelectedIndex = i;
          break;
          }
        }

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Actualiza la información de los bloques, en la escena para que reflejen el indice del bloque en lugar del ID
    private void UpdateBlocksInfo()
      {
      for( int i=0; i<Escena.Blocks.Count; i++ )                                // Recorre todos los bloques que hay en la escena
        {
        var bck = Escena.Blocks[i];                                             // Bloque actual

        Escena.SetCell( bck.Col, bck.Row, new CVal( Cell.Bloque, i ) );         // Coloca el indice en la celda, en lugar del ID

        GameZone.Children.Add( bck.Ctrl );                                      // Adiciona el control a la zona de juego (para que se vea)
        }

      SetBlocksInfo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Recorre la escena y crea los bloques y target basados en su contenido, tambien carga la imagen de fondo
    private void UpdateObjsFromEscena()
      {
      OnClickShowFondo(null,null);                                              // La pone en la pantalla

      for( int row=0; row<Escena.Rows; ++row )                                  // Recorre todas las filas
        for( int col=0; col<Escena.Cols; col++ )                                // Recorre todas las columnas para cada fila
          {
          var Val = Escena.GetCell( col, row );                                 // Obtiene el contenido de la celda

          if( Val.Tipo == Cell.Pared  ) CreateParedObj ( col, row );            // Si es pared crea objeto y lo pone en pantalla
          if( Val.Tipo == Cell.Target ) CreateTargetObj( col, row );            // Si es traget crea el objeto y lo pone en pantalla
          }
      }

    string SnapShot = null;
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Intercambia la interface entre el modo jugar y el modo diseño
    private void SetGameMode (bool game=true )
      {
      if( game )                                                                // Si esta entrando en el modo jugar
        { 
        BtnsDesing.Visibility = Visibility.Hidden;                              // Oculta botones del modo diseño
        BtnsPlay.Visibility   = Visibility.Visible;                             // Muestra botones del modo jugar    
        PlayData.Visibility   = Visibility.Visible;                             // Muestra datos en la parte inferior

        SetMode( Mode.Play );                                                   // Pone el modo de trabajo en juagar
        UpdatePlayData();                                                       // Actualiza datos del juego en la parte inferior

        SnapShot = Escena.ToXml( true );                                        // Crea una capia de la escena en este momento

        AnimatePath.SolutionChanged = false;
        }
      else                                                                      // Se esta retornado el modo diseño
        {
        if( AnimatePath.SolutionChanged )
          {
          var Ret = MessageBox.Show(this, "Hay una solución que no ha sido guardada", "", MessageBoxButton.OKCancel );
          if( Ret == MessageBoxResult.Cancel ) return;
          }

        BtnsDesing.Visibility = Visibility.Visible;                             // Muestra botones del modo diseño
        BtnsPlay.Visibility   = Visibility.Hidden;                              // Oculta botones del modo jugar      
        PlayData.Visibility   = Visibility.Hidden;                              // Oculta los datos de la parte inferior

        AnimatePath.ClearUndo();                                                // Borra todas las operaciones del Undo
        SetMode( Mode.Cursor );                                                 // Pone el modo de trabajo cursor (Diseño)

        LoadEscenaFromXml( SnapShot );                                          // Restaura la escena al momento que se empezo a juagar
        }

      for( int i=0; i<Targets.Count; i++ )                                      // Cambia el cursor de todos los targets
        {
        Targets[i].Cursor      = (game)? Cursors.Arrow : Cursors.Hand;          
        Targets[i].ForceCursor = !game;
        }

      Escena.ShowCursor( !game );                                               // Cambia el cursor del pusher y todos los bloques
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Actualiza los datos del juego en la parte inferior
    private void UpdatePlayData()
      {
      txtSolut.Text = AnimatePath.SolutionText();
      txtMoves.Text = AnimatePath.MovesText();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Inicia el modo jugar para comenzar a probar la escena
    private void OnInitGame(object sender, MouseButtonEventArgs e)
      {
      ContAnim = false;
      EscenaDef.Show( false );                                                  // Oculta panel de definicion de escena si esta abierto

      SetGameMode( true );                                                      // Cambia la interface a modo jugar
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Detiene el modo jugar y retorna al modo diseño de escena
    private void OnStopGame(object sender, MouseButtonEventArgs e)
      {
      if( AnimObj!=null && AnimObj.InCurse() ) return;                          // Si es esta en medio de una animación no hace nada

      SetGameMode( false );                                                     // Cambia toda la interface a modo diseño
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Deshace el ultimo movimiento en el modo jugar
    private void OnUndoMove(object sender, MouseButtonEventArgs e)
      {
      if( AnimObj!=null && AnimObj.InCurse() )                                // Si es esta en medio de una animación no hace nada
        { 
        AnimatePath.PauseAnim = true;
        ContAnim = true;
        return;
        }

      ContAnim = false;
      AnimatePath.Undo();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Reincia la escena en el modo jugar
    private void OnResetEscena(object sender, MouseButtonEventArgs e)
      {
      if( AnimObj!=null && AnimObj.InCurse() )                              // Si es esta en medio de una animación no hace nada
        { 
        AnimatePath.PauseAnim = true;
        ContAnim = true;
        return;
        }

      ContAnim = false;
      AnimatePath.ClearUndo();
      LoadEscenaFromXml( SnapShot );                                            // Restaura la escena al momento que se empezo a juagar

      txtMoves.Text = AnimatePath.MovesText();                                  // Actualiza cantidad de movimientos realizados
      }

    bool ContAnim;     // Flag para definir si se continua una animación o es una nueva
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Muestra la solución de la escena si ya esta determinada
    private void OnSolucion(object sender, MouseButtonEventArgs e)
      {
      if( AnimObj!=null && AnimObj.InCurse() )
        {
        AnimatePath.PauseAnim = true;
        ContAnim = true;
        return;   
        }

      if( AnimatePath.Solution == null )
        {
        MessageBox.Show("No se a llegado a ninguna solución para esa escena");
        return;
        }

      if( !ContAnim )                                                           // Si no es una continuación de una animación
        LoadEscenaFromXml( SnapShot );                                          // Restaura la escena al momento que se empezo a juagar

      AnimObj = new AnimatePath( AnimatePath.Solution.ToArray(),
                                 AnimatePath.Solution.Count, 
                                 true, ContAnim==false );                        // Anima el desplazamientos según el camino definido
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cuando cambia el tamaño de la ventana
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
      {
      ScaleGameZona();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cuando cambia el tipo de fondo a utiliza para rellenar la pantalla
    private void selFondo_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
      var idx = selFondo.SelectedIndex;
      if( idx<0 ) return;

      var txt = (string)((ComboBoxItem)selFondo.Items[idx]).Content;

      Escena.sFillFondo = txt + ".jpg";
      }

    } //=============================================================================================================================================================
  }
