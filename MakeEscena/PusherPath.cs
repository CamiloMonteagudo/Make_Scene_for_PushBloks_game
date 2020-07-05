using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  // Sentido de los segmentos de restas usados en PusherPath
  public enum SegmSent:byte
    {
    Indef = 0,
    Horz  = 1,
    Vert  = 2,
    }

  //==============================================================================================================================================================
  /// <summary> Almacena los datos asociados a un punto del camino del pusher </summary>
  public struct PathPnt
    {
    public Point     pnt;                                                 // Posición en coordenadas graficas
    public SegmSent  Sent;                                                // Sentido del segmento Horizontal, Vertical e Indefinido
    public int       Sgn;                                                 // Signo del recorrido (1) Abajo/Derecha (-1) Arriba/Izquierda  
    public int       Col;                                                 // Posición en coordenadas de escena (fila/columna)
    public int       Row;
    public bool      Bck;                                                 // Indica si se empuja un bloque o no

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Establece todos los datos del punto
    public void Set( int col, int row, SegmSent sent, int sgn, bool bck )
      {
      pnt.X = Escena.GetXM( col );                                        // Determina el punto medio de la celda
      pnt.Y = Escena.GetYM( row );

      Col = col;
      Row = row;

      Sent = sent;
      Sgn  = sgn;
      Bck  = bck;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cambia la columna del donde esta el punto
    public void ChangeCol( int col )
      {
      Col = col;
      pnt.X = Escena.GetXM( col );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Cambia la fila del donde esta el punto
    public void ChangeRow( int row )
      {
      Row = row;
      pnt.Y = Escena.GetYM( row );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    internal bool IsVert() { return (Sent==SegmSent.Vert); }
    internal bool IsHorz() { return (Sent==SegmSent.Horz); }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene una representación del punto en forma de cadena de caracteres
    public override string ToString()
      {
      return Col.ToString() + "," + Row + "," + Bck;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Lee desde una cadena de caracteres los datos del punto
    public static PathPnt FromString( string sData )
      {
      var pnt  = new PathPnt();                                       // Crea un punto vacio, por defecto

      var tokens = sData.Split(',');                                  // Divide las subcadenas separadas por ,
      if( tokens.Length < 3 ) return pnt;                             // Si no tiene 3 elementos, la cadena no es valida

      try 
	      {	        
		    pnt.Col =  int.Parse( tokens[0] );                            // Primer elemnto columna 
		    pnt.Row =  int.Parse( tokens[1] );                            // Segundo elemento fila
		    pnt.Bck = bool.Parse( tokens[2] );                            // Tercer elmento, si se empuja un bloque o no

        pnt.Sgn = 10;                                                 // Le pone un 10 al signo para marcar el punto como inicializado
        return pnt;                                                   // Retorna el punto con los datos leidos
	      }
	    catch { return pnt; }                                           // Hubo algún error al convertir los valores
      }

    } //=============================================================================================================================================================

  //----------------------------------------------------------------------------------------------------------------------------------------------------------
  /// <summary> Clase para manejar y almacenar el camino que debe recorrer el pusher </summary>
  static public class PusherPath
    {
    static public PathPnt[] Items =  new PathPnt[80];                     // Arreglo con los puntos del camino
    static public int       Count = 0;                                    // Numero de puntos que forman el camino

    static Point    StartPoint;                                           // Punto de referencia para desplazamineto del mouse                                    
    static PathPnt  EndPnt;                                               // Punto final del camino

    static PolyLineSegment Lineas;                                        // Objetos de WPF para dibujar
    static PathFigure      Figura; 
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Inicializa control donde se va dibujar el camino, obtine referencia para esta clase
    static public void Init( Path ctrl )
      {
      var geom = new PathGeometry();
      Figura   = new PathFigure();
      Lineas   = new PolyLineSegment();

      Figura.Segments.Add( Lineas );
      geom.Figures.Add( Figura );

      ctrl.Data = geom;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Comienza la definición de un perfil nuevo
    static public void SetInitPoint( Point pnt )
      {
      Lineas.Points.Clear();                                              // Borra de pantalla todos puntos que habia
      StartPoint = pnt;                                                   // Pone punto de refernencia para desplazamiento del mouse

      int Col = Escena.Pusher.Col;                                        // Obtine posicón del pusher en coordenas de escena
      int Row = Escena.Pusher.Row;

      Count = 1;
      Items[0].Set( Col, Row, SegmSent.Indef, 0, false );                 // Define primer punto coicidiendo con el pusher

      var X = Escena.GetXM( Col );                                        // Obtine posicón media del pusher en pixeles
      var Y = Escena.GetYM( Row );

      Figura.StartPoint = new Point( X, Y );                              // Pone punto inicial de la figura, donde esta el pusher
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Establece un nuevo punto de referencia para el desplazamiento del mouse
    static public void ContinuePath( Point pnt )
      {
      StartPoint = pnt;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Dibuja todos los segmentos del camino definidos en el momento de llamar la función
    static public void Draw( )
      {
      Lineas.Points.Clear();                                              // Quita todas los puntos definidos anteriormente
      for( int i=0; i<Count; i++ )                                        // Recorre todos los puntos
        Lineas.Points.Add( Items[i].pnt );                                // Va adicionado, uno por uno
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static int dxDif = 0;                                                 // Cantidad de puntos sobrantes en cada eje del punto anterior 
    static int dyDif = 0;                                                
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    static public void AddPoint( Point pnt )
      {
      if( Count < 1 ) return;

      int dx = (int)(pnt.X-StartPoint.X) + dxDif;                         // Desplazamiento respeto al último punto analizado
      int dy = (int)(pnt.Y-StartPoint.Y) + dyDif;

      int absdx = Math.Abs(dx);                                           // Obtiene desplazamientos absolutos
      int absdy = Math.Abs(dy);

      if( absdx<Escena.zCell && absdy<Escena.zCell ) return;              // No se movio respecto a la ultima celda analizada

      StartPoint = pnt;                                                   // Fija punto se referencia para la proxima vez

      SegmSent Sent = (absdx>absdy)? SegmSent.Horz : SegmSent.Vert;       // Determina el sentido de desplazamiento

      EndPnt = Items[ Count-1 ];                                          // Obtiene el ultimo punto definido del camino

      bool ret;
      if( Sent == SegmSent.Vert )                                         // Si la sentido es Vertical
        {
        int sgn = (dy<0)? -1 : 1;                                         // Signo del desplazamiento en la vertical

        dxDif = 0;                                                        // Desprecia desplazamientos en la horizontal
        dyDif = dy - sgn * Escena.zCell;                                  // Solo toma el tamaño de una celda, deja resto para la proxima llamada

        if( EndPnt.IsVert() ) ret = ChangeVert( sgn );                    // Ultimo segmento era vertical, lo cambia
        else                  ret = AddNewVert( sgn );                    // Ultimo segmento era horizontal, adiciona uno nuevo vertical
        }
      else                                                                // La dirección es Horizontal
        {
        int sgn = (dx<0)? -1 : 1;                                         // Signo del desplazamiento en la horizontal

        dxDif = dx - sgn * Escena.zCell;                                  // Toma tamaño de una celda, el resto lo deja para proxima llamada
        dyDif = 0;                                                        // Despresia despalazamiento en la vertical

        if( EndPnt.IsHorz() ) ret = ChangeHorz( sgn );                    // Ultimo segmento era horizontal, lo cambia 
        else                  ret = AddNewHorz( sgn );                    // Ultimo segmento era vertical, adiciona uno nuevo horizontal
        }

      Draw();                                                             // Dibuja el camino
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra el camino de la pantalla
    static public void Clear()
      {
      Lineas.Points.Clear();
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se adiciona un nuevo segamento Vertical
    private static bool AddNewVert(int sgn)
      {
      if( EndPnt.Bck ) return false;                                      // Si se esta empujando, no se admiten nuevos segmentos

      int   row = EndPnt.Row + sgn;                                       // Obtiene la fila, relativo al ultimo punto
      bool  bck = false;                                                  // Asume que no es esta empujando un bloque
      if( !VerifyVert( sgn, row, ref bck ) ) return false;                // Verifica si la celda es valida

      Items[Count++].Set( EndPnt.Col, row, SegmSent.Vert, sgn, bck );     // Incrementa cantidad de segmentos y pone los datos
      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Realiza un cambio en el desplazamiento Vertical (Acorta, Alarga o Elimina el ultimo segmento)
    private static bool ChangeVert(int sgn)
      {
      int  row = EndPnt.Row + sgn;                                        // Incrementa/decrementa fila, según el signo
      bool bck = EndPnt.Bck;                                              // Si se esta empujando un bloque

      if( Count>1 && Items[Count-2].Row==row )                            // Si el segmento se hace de logitud 0
        {
        --Count;                                                          // Ignora el ultimo segmento
        return true;                                                      // Termina
        }

      if( (EndPnt.Sgn==sgn || EndPnt.Sgn==0) &&                           // Si se esta alargando el segmento
          !VerifyVert( sgn, row, ref bck )    ) return false;             // Verifica que la nueva celda sea valida

      if( bck && !EndPnt.Bck )                                            // Si se comenzo a empujar un bloque
        Items[Count++].Set( EndPnt.Col, row, SegmSent.Vert, sgn, bck );   // Crea un nuevo segmento
      else
        Items[ Count-1 ].ChangeRow( row );                                // Cambia la fila del ultimo segmento

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Verifica que el desplazamiento Vertical se realice hacia una celda valida
    private static bool VerifyVert( int sgn, int row, ref bool bck )
      {
      if( bck ) row += sgn;                                               // Si esta empujando un bloque, se chequea una celda por delante

      if( row<0 || row>=Escena.Rows ) return false;                       // La celda esta fuera del area de la escena

      var Val = Escena.GetCell( EndPnt.Col, row );                        // Obtiene el contenido de la celda

      if( Val.IsPared() ) return false;                                   // Si es una pared, no es valido el movimiento

      if( Val.IsBloque() )                                                // Es un bloque
        { 
        if( bck ) return false;                                           // Ya se estaba empujando, no valido mas de un bloque a la vez

        bck = true;                                                       // Retorna que se comenzo a empujar un bloque
        return VerifyVert( sgn, row, ref bck );                           // Chequea un punto por adelantado, para ver si es valido
        }

      return true;                                                        // Retorna que es valido el movimiento
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Se adiciona un nuevo segamento Horizontal
    private static bool AddNewHorz(int sgn)
      {
      if( EndPnt.Bck ) return false;                                      // Si se esta empujando no se admiten segmentos nuevos

      int   col = EndPnt.Col + sgn;                                       // Obtiene columna relativa al ultimo punto
      bool  bck = false;                                                  // Asume que no se va a empujar ningún bloque
      if( !VerifyHorz( sgn, col, ref bck ) ) return false;                // Verifica si la celda es valida

      Items[Count++].Set( col, EndPnt.Row, SegmSent.Horz, sgn, bck );     // Adiciona nuevo segmento y pone sus datos
      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Realiza un cambio en el desplazamiento horizontal (Acorta, Alarga o Elimina el ultimo segmento)
    private static bool ChangeHorz(int sgn)
      {
      int  col = EndPnt.Col + sgn;                                        // Incrementa/decrementa columna, según el signo
      bool bck = EndPnt.Bck;                                              // Si se esta empujando un bloque

      if( Count>1 && Items[Count-2].Col==col )                            // Si el segmento se hace de logitud 0
        {
        --Count;                                                          // Ignora el ultimo segmento
        return true;                                                      // Termina
        }

      if( (EndPnt.Sgn==sgn || EndPnt.Sgn==0) &&                           // Si se esta alargando el segmento
          !VerifyHorz( sgn, col, ref bck )    ) return false;             // Verifica que la nueva celda sea valida

      if( bck && !EndPnt.Bck )                                            // Si se comenzo a empujar un bloque
        Items[Count++].Set( col, EndPnt.Row, SegmSent.Horz, sgn, bck );   // Crea un nuevo segmento
      else
        Items[ Count-1 ].ChangeCol( col );                                // Cambia la columna del ultimo segmento

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Verifica que el desplazamiento horizontal se realice hacia una celda valida
    private static bool VerifyHorz(int sgn, int col, ref bool bck )
      {
      if( bck ) col += sgn;                                               // Si esta empujando un bloque, se cheque una celda por delante

      if( col<0 || col>=Escena.Cols ) return false;                       // La celda esta fuera del area de la escena

      var Val = Escena.GetCell( col, EndPnt.Row );                        // Obtiene el contenido de la celda

      if( Val.IsPared() ) return false;                                   // Si es una pared, no es valido el movimiento

      if( Val.IsBloque() )                                                // Es un bloque
        { 
        if( bck ) return false;                                           // Ya se estaba empujando, no valido mas de un bloque a la vez

        bck = true;                                                       // Retorna que se comenzo a empujar un bloque
        return VerifyHorz( sgn, col, ref bck );                           // Chequea un punto por adelantado, para ver si es valido
        }

      return true;                                                        // Retorna que es valido el movimiento
      }

    } //=============================================================================================================================================================

  }
