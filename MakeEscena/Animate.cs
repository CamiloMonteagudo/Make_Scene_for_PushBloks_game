using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  /// <summary> Estructura para gurdar los datos para hacer Undo </summary>
  public struct UndoInfo
    {
    public double             Off;                                         // Offset para la traslación
    public DependencyProperty Prop;                                        // Propiedad que se va a animar
    public TimeSpan           Time;                                        // Tiempo empleado para la traslación 

    public int Col;
    public int Row;                                                        // Posición del pusher
    public int Sign;

    public double Angle1;                                                  // Angulo inicial y final de rotación 
    public double Angle2;

    public int BckIdx;                                                     // Indice del bloque que empuja el pusher (-1 no empueja)
    }

  //==============================================================================================================================================================
  /// <summary> Maneja los detalles para la animación del movimiento del del pusher y de los bloques a lo largo un camino </summary>
  public class AnimatePath
    {
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    PathPnt       Segm;                                                         // Segmento del camino que se esta animando
    static double Speed = 200.0;                                                // Velocidad de desplazamiento en pixels/segundos
    static int    IdxSegm = 1;                                                  // Segmento actual, se inicia en 1 (en 0 esta el pusher)

    static public Action<int> EndAnimatedMove;                                  // Evento que se llama cuando termina una animación, el parameto
                                                                                // indica 0-Noramal, 1-Undo, 2-Reproducción

    static RotateTransform    AnimRotate;                                       // Transformación para rotar el pusher
    static TranslateTransform AnimTranslate;                                    // Transformación para desplazar el pusher

    PathPnt[] Items;
    int       Count;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static public List<PathPnt>  Replay   = new List<PathPnt>();                // Lista de reproducción de la escena hasta ese momento
    static public List<UndoInfo> UndoList = new List<UndoInfo>();               // Información para deshacer las jugadas
    static public List<PathPnt>  Solution = null;                               // Ultima solución alcanzada

    static public bool   PauseAnim = false;                                     // Bandera para indicar que la animación esta detenida
    static public double AmimTime = 0.2;                                        // Tiempo de la secuencia de animación del pusher

    bool Reprod;                                                                // Indica que se esta reproduciendo una escena, en lugar de ejecutandola
    static public bool SolutionChanged;                                         // Indica que cambio la solución de la escena

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Invalida la solucion
    static public bool InvalideSolution()
      {
      if( Solution != null )
        {
        //var Ret = MessageBox.Show("Hay una solución que se va a pareder, ¿Desea continuar?", "", MessageBoxButton.YesNo );
        //  if( Ret == MessageBoxResult.No ) return false;

        Solution = null;
        }

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Inicializa los parametros para la animación
    public AnimatePath( PathPnt[] Segmentos, int count, bool reprod, bool ForInit=true )
      {
      if( ForInit ) IdxSegm = 1;                                                // Si la animación de realiza desde el principio

      Reprod = reprod;                                                          // Indica que es una reprodución de los movimientos
      Items  = Segmentos;                                                       // Arreglo con los segmentos a animar
      Count  = count;                                                           // Número de segmentos en el arreglo

      if( Reprod && IdxSegm==1 ) UndoList.Clear();                              // Si esta en modo reprodución borra lista de undo

      var grp = (TransformGroup) Escena.Pusher.RenderTransform;                 // Grupo de trasformacines asociadas al pusher

      AnimRotate    = (RotateTransform) grp.Children[0];                        // La primera es la transformación para rotación
      AnimTranslate = (TranslateTransform) grp.Children[1];                     // LA segunda es la transformación para traslación

      AnimRotate.CenterX = Escena.zCell/2;                                      // Pone centros de rotación en el cento de la celda
      AnimRotate.CenterY = Escena.zCell/2;

      if( IdxSegm>0 && IdxSegm<Count )                                          // Si hay al menos dos puntos
        {
        PauseAnim = false;
        var anim = new Int32Animation()                                         // Crea una animación para la secuencia de imagenes de caminar
          {
          From = 0,                                                             // Empieza desde la primera imagen
          To   = Escena.Pusher.BitMaps.Count-1,                                 // Hasta el número de imagenes de la seguencia
          Duration = TimeSpan.FromSeconds(AmimTime),                            // Duración de la secuencia de animación de todos los cuadros
          //AutoReverse = true,                                                   // Regresa a la posición inicial
          RepeatBehavior = RepeatBehavior.Forever,                              // Por toda la animación
          };

        Escena.Pusher.BeginAnimation( CtrlPush.IdxImgProperty, anim );          // Comienza animación de caminar

        Segm = Items[ IdxSegm ];                                                // Inicia con el primer segmento

        if( Replay.Count==0 )                                                   // Si es el primer movimiento de la escena
          Replay.Add( Items[0] );                                               // Adiciona punto inicial a la solución

        RotatePusher();                                                         // Comienza rotando el pusher, si es necesario
        }
      else                                                                      // Si no hay ningún segmento
        if( EndAnimatedMove != null ) EndAnimatedMove( Reprod? 2: 0 );          // Llama al evento de terminación de la animación

      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Llamada al final de la animación de un segmento, para continuar con el proximo, hasta el final que llama al vento de fin de animación
    private void AnimateNextSegment(object sender, EventArgs e)
      {
      EndAnimateSegmet();                                                       // Posiciona los elementos después de la animación del segmento

      ++IdxSegm;                                                                // Pasa al proximo segmento
      if( IdxSegm< Count && !PauseAnim )                                          // Si todavia quedan puntos en el arreglo y no se ha detenido la animación
        {
        Segm = Items[ IdxSegm ];                                                // Pone segmento actual
        RotatePusher();                                                         // Comienza con la rotación del pusher
        }
      else                                                                      // Se terminaron los puntos del camino
        {
        PauseAnim = false;

        Escena.Pusher.BeginAnimation( CtrlPush.IdxImgProperty, null );          // Termina la animación de caminar
        Escena.Pusher.IdxImg = 0;                                               // Pone la primera imagen de la secuencia

        if( EndAnimatedMove != null ) EndAnimatedMove( Reprod? 2: 0 );          // Llama al evento de terminación de la animación
        }
      }

    double Angle1, Angle2;
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Rota el pucher, de acuardo a la dirección que va a seguir en el recorrido del segmento
    private void RotatePusher()
      {
      if( Segm.IsHorz() ) Angle2 = (Segm.Sgn == 1)?  0.0 : 180.0 ;              // Determina el angulo, según de orientación y el signo
      else                Angle2 = (Segm.Sgn == 1)? 90.0 : 270.0;

      Angle1 = AnimRotate.Angle;                                                // Obtiene el angulo que esta
      Angle1 = (Angle1<0)? 360+Angle1 : ((Angle1==360)? 0 : Angle1);            // Normaliza el angulo

      if( Angle1==Angle2 )                                                      // Si esta en el mismo angulo
        TranslatePusher( null, null );                                          // Procesa directamente la traslación del pusher
      else
        RotateAnimated( Angle1, Angle2, new EventHandler(TranslatePusher) );    // Anima la rotación y al final manda a traladar
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Anima la rotación del pusher entre los angulos Ang1 y Ang2, y opcionelmente llama un evento al finalizar
    private static void RotateAnimated( Double Ang1, Double Ang2, EventHandler AtEnd = null )
      {
      var Diff = (Ang1 - Ang2);                                                 // Obtiene angulo relativo al anterior
      if( Diff == -270 ) Ang2 = -90;                                            // Pone el sentido para el camino mas corto
      if( Diff ==  270 ) Ang2 = 360;

      var Durac = TimeSpan.FromSeconds( (Math.Abs(Diff)>90)? 0.3 : 0.1 );       // Duraión de la animación según el angulo a rotar
      var Anim1 = new DoubleAnimation( Ang1, Ang2, Durac );                     // Animación del valor del angulo a rotar

      if( AtEnd != null  ) Anim1.Completed += AtEnd;                            // Si tiene que hacer algo al final, pone evento

      AnimRotate.BeginAnimation( RotateTransform.AngleProperty , Anim1 );       // Comienza la animación de la rotación
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static double              TraslOff;                                               // Offset para la traslación
    static DependencyProperty  TraslProp;                                              // Propiedad que se va a animar
    static TimeSpan            TraslTime;                                              // Tiempo empleado para la traslación 
    static int                 TraslRow;                                               // Tiempo empleado para la traslación 
    static int                 TraslCol;                                               // Tiempo empleado para la traslación 

    static BlockData           LstBlck = null;                                         // Bloque que se esta empujando
    static int                 LstIdx  = -1;                                           // Indice del bloque que se este empujando
    static int                 TraslSign; 
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Anima la traslación del pusher a lo largo del segmento actual, si hay un bloque lo desplaza
    private void TranslatePusher(object sender, EventArgs e)
      {
      LstBlck = null;                                                           // Asume que no hay bloque para desplazar
      LstIdx  = -1;

      TraslCol = Escena.Pusher.Col;
      TraslRow = Escena.Pusher.Row;

      if( Segm.IsHorz() )                                                       // El segmento es horizontal
        {
        TraslOff = Escena.DistX( Segm.Col - Escena.Pusher.Col );                // Obtiene desplazamiento en x
        TraslProp = TranslateTransform.XProperty;                               // Propieda para la aimación es X
        }
      else
        { 
        TraslOff  = Escena.DistY( Segm.Row - Escena.Pusher.Row );               // Obtiene desplazamiento en Y
        TraslProp = TranslateTransform.YProperty;                               // Propieda para la aimación es Y
        }

      var seg = Math.Abs( TraslOff ) / Speed;                                   // Calcula el tiempo según la velocidad
      if( seg==0 ) seg = 0.1; 
 
      TraslTime = TimeSpan.FromSeconds( seg );
      var Anim = new DoubleAnimation( 0, TraslOff, TraslTime );                 // Anima el valor de la propiedad

      Anim.Completed += AnimateNextSegment;                                     // Pasa al otro segmento cuando termine la traslación
        
      if( Segm.Bck ) TranslateBlock( Segm.Sgn );                                // Si hay empuejar un bloque, lo hace en paralelo

      AnimTranslate.BeginAnimation( TraslProp, Anim );                          // Comienza la animación del desplazamiento
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Desplaza el bloque que se encuentre delante del pusher
    private static void TranslateBlock( int Sgn)
      {
      TraslSign = Sgn;
      var IsX = ( TraslProp==TranslateTransform.XProperty );                    // Determina en que eje es el despalzamiento

      var col = Escena.Pusher.Col + (IsX? Sgn : 0   );                          // Destermina la celda según el signo y el eje
      var row = Escena.Pusher.Row + (IsX? 0   : Sgn );

      var Val = Escena.GetCell( col, row );                                     // Obtiene el contenido de la celda
      if( Val.Tipo != Cell.Bloque ) return;                                     // Si no es un bloque, no hace nada

      LstIdx  = Val.Info;                                                       // Guardo el indice para usarlo posteriormente
      LstBlck = Escena.Blocks[ LstIdx ];                                        // Obtiene bloque según información de la celda

      var Translate = new TranslateTransform();                                 // Crea una trandformación de traslación
      LstBlck.Ctrl.RenderTransform = Translate;                                 // La asocia al bloque a desplazar

      var Anim = new DoubleAnimation( 0, TraslOff, TraslTime );                 // Valor de animación, con lo mismos parametros que el pusher
      Translate.BeginAnimation( TraslProp, Anim );                              // Comienza la traslación del bloque
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Hace arreglos finales, en la animación de un segmento, quita las trasformaciones y fija las posiciones
    public void EndAnimateSegmet()
      {
      AddUndoInfo();

      EndBlockAnimate();
      EndPuherAnimate( Segm.Col, Segm.Row );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se llama al final del animación del pusher, para establecer su posición en la escena y restaurar las propiedades
    private static void EndPuherAnimate( int col, int row )
      {
      AnimTranslate.BeginAnimation( TranslateTransform.XProperty, null );       // Quita transformación de traslación del pusher
      AnimTranslate.BeginAnimation( TranslateTransform.YProperty, null );

      Escena.Pusher.Move( col, row );                                           // Ajusta todos los parametros del pusher para la nueva posición
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se llama al final del animación de un bloque, para establecer su posición en la escena y restaurar las propiedades
    private static void EndBlockAnimate()
      {
      if( LstBlck == null ) return;                                             // No se desplazo un bloque, no hace nada

      var Transl = (TranslateTransform)LstBlck.Ctrl.RenderTransform;            // Guarda trasformación de traslación asociada al bloque

      LstBlck.Ctrl.RenderTransform = null;                                      // Quita la transformación del bloque

      var col = LstBlck.Col + Escena.DistCol(Transl.X);                         // Obtiene celda donde quedo el bloque
      var row = LstBlck.Row + Escena.DistRow(Transl.Y);

      LstBlck.Move( col, row );                                                 // Ajusta todos los datos del bloque y la escena para esa celda

      LstBlck = null;
      LstIdx  = -1;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Adiciona información para poder deshacer la operación
    private void AddUndoInfo()
      {
      var undo = new UndoInfo();                                                // Crea objeto para guadar información

      undo.Off    = TraslOff;                                                   // Guarda todo los datos
      undo.Prop   = TraslProp;
      undo.Time   = TraslTime;
      undo.BckIdx = LstIdx;
      undo.Col    = TraslCol;
      undo.Row    = TraslRow;
      undo.Sign   = TraslSign; 
      undo.Angle1 = Angle1;
      undo.Angle2 = Angle2;

      UndoList.Add( undo );                                                     // Adiciona los datos a la lista
      Replay.Add( Segm );                                                       // Adiciona el segmento a la lista
      }

    static bool InUndo;
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Deshece la ultima animación realizada
    static public void Undo()
      {
      var idx = UndoList.Count -1;                                              // Obtiene indice a los ultimos datos del undo
      if( idx < 0 || InUndo ) return;                                           // Si no hay datos retorn sin hacer nada o ya esta en Undo

      InUndo = true;
      var undo = UndoList[idx];                                                 // Obtiene datos del undo

      if( undo.BckIdx != -1 )                                                   // Se empujo un bloque
        {
        TraslOff  = -undo.Off;                                                  // Pone el recorrido inverso al original
        TraslTime = undo.Time;                                                  // Pone el tiempo
        TraslProp = undo.Prop;                                                  // Pone la propiedad (x o y)
        
        TranslateBlock( undo.Sign );                                            // Anima movimieto inverso
        }

      var Anim = new DoubleAnimation( 0, -undo.Off, undo.Time );                // Anima desplazamiento inverso del pusher
      Anim.Completed += EndUndo;                                                // Pone evento para cuando termine

      AnimTranslate.BeginAnimation( undo.Prop, Anim );                          // Comienza la traslación aimada del pusher
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra todas las operaciones que hay en la lista de Ondo
    static public void ClearUndo()
      {
      UndoList.Clear();                                                         // Borra la lista de undo
      Replay.Clear();                                                           // Borra la lista de jugadas realizadas

      MovesText();
      }                                                                         //

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se llama cuando se termine de hacer en undo animado del pusher y de los bloques
    private static void EndUndo(object sender, EventArgs e)
      {
      var idx = UndoList.Count -1;                                              // Obtiene indice al ultimo elemento del undo
      var undo = UndoList[idx];                                                 // Obtiene los datos del elemento

      EndPuherAnimate( undo.Col, undo.Row );                                    // Termina la ainimación del pusher
      EndBlockAnimate();                                                        // Termina la animación del bloque, si lo hubo

      if( undo.Angle1 != undo.Angle2 )                                          // Si el angulo de rotación cambio
        RotateAnimated( undo.Angle2, undo.Angle1 );                             // Anima la rotación en sentido inverso

      UndoList.RemoveAt( idx );                                                 // Remueve el elemento de la lista de undo
      Replay.RemoveAt( Replay.Count-1 );                                        // Remueve ultimo punto de la lista de replay

      if( EndAnimatedMove != null ) EndAnimatedMove(1);                         // Llama al evento de terminación de la animación

      InUndo = false;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Función para debuger los elementos de una lista de puntos (Camino para aimación del pusher)
    private static void DumpLst( List<PathPnt> Lst )
      {
      Console.WriteLine("\r\ni\tCol\tRow\tSent\tSgn\tBck ");
      for( int i=0; i<Lst.Count; i++ )
        {
        var elm = Lst[i];

        Console.WriteLine( i.ToString() + "\t" + elm.Col + "\t" + elm.Row + "\t" + elm.Sent  + "\t" + elm.Sgn  + "\t" + elm.Bck );
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Guarda la Solución de la escena
    internal static void SaveSolution()
      {
      int NMove1 = GetMoveCount( Replay   );
      int NMove2 = GetMoveCount( Solution ); 

      if( NMove2>0 && NMove2<NMove1 )
        {
        MessageBox.Show( "Ya hay una solución mejor que la encontrada" );
        return;
        }

      Solution = new List<PathPnt>(Replay);
      SolutionChanged = true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Retorna la cantidad de movimientos de la solucion
    static public int GetSolutionMoves()
      {
      return GetMoveCount( Solution ); 
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cuenta la cantidad de movidas de la lista de puntos representada por Lst
    static public int GetMoveCount(List<PathPnt> Lst)
      {
      if( Lst==null || Lst.Count<=1 ) return 0;

      int n = 0;
      int lstCol = Lst[0].Col;
      int lstRow = Lst[0].Row;

      for( int i=1; i<Lst.Count; i++ )
        {
        n += Math.Abs( lstCol - Lst[i].Col );
        n += Math.Abs( lstRow - Lst[i].Row );

        lstCol = Lst[i].Col;
        lstRow = Lst[i].Row;
        }
      
      return n;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Deermina si en el ultimo moviemiento se posiciono una celda sobre un traget
    static public bool SetInTraget()
      {
      var idx = UndoList.Count -1;                                              // Obtiene indice al ultimo elemento del undo
      if( idx < 0 ) return false;

      var BckIdx = UndoList[idx].BckIdx;                                        // Obtiene el indice al bloque empujado
      if( BckIdx==-1 ) return false;                                            // No se empujo ningun bloque, retouna falso

      return ( Escena.Blocks[BckIdx].On.Tipo == Cell.Target );                  // Si el bloque esta sobre un traget
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Retorna un texto con información de la solución
    internal static string SolutionText()
      {
      if( Solution == null )
        return "No hay";
      else
        return GetMoveCount( Solution ).ToString();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Retorna un texto con la cantidad de mosvimientos realizados hasta el momento
    internal static string MovesText()
      {
      return GetMoveCount( Replay ).ToString();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Determina si la animación esta en curso o no
    internal bool InCurse()
      {
      if( Count < 1 ) return false;
      if( IdxSegm< 1 || IdxSegm>=Count ) return false;

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    //private static void TranslatePusher2(object sender, EventArgs e)
    //  {
    //  double from, to;
    //  DependencyProperty Prop;
 
    //  if( Segm.IsHorz() )
    //    {
    //    from = Canvas.GetLeft( Pusher.Ctrl );
    //    to   = GetX(Segm.Col);
    //    Prop = Canvas.LeftProperty;
    //    }
    //  else
    //    { 
    //    from = Canvas.GetTop( Pusher.Ctrl );
    //    to   = GetY(Segm.Row);
    //    Prop = Canvas.TopProperty;
    //    }

    //  var seg = Math.Abs( from-to ) / Speed;
 
    //  var Anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seg) );

    //  if( CompletedEvent!=null ) Anim.Completed += CompletedEvent;
        
    //  Pusher.Ctrl.BeginAnimation( Prop, Anim, HandoffBehavior.Compose );
    //  //Pusher.Move( Segm.Col, Segm.Row );
    //  }

    } //=============================================================================================================================================================
  }
