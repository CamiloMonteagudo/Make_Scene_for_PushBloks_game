using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace MakeEscena
  {
  //==============================================================================================================================================================
  // Tipos de celdas que pueden haber
  public enum Cell:byte
    {
    Piso   = 0x00,
    Pared  = 0xA0,
    Bloque = 0x50,
    Target = 0x10,
    Pusher = 0xF0,
    }

  //==============================================================================================================================================================
  // Tipos de objetos que se dibujan sobre la zona de juego
  [Flags]public enum DrawTipo
    {
    Grid   = 0x0001,
    Block  = 0x0002,
    Target = 0x0004,
    Pared  = 0x0008,
    All    = 0xffff,
    }

  //==============================================================================================================================================================
  // Estructura para manejar los valores que puede tener una celda
  public struct CVal
    {
    byte Val;

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Crea un nuevo valor, con un entero, con un tipo de celda y con un tipo de celda y la información asociada
    public CVal( int val  ) { Val = (byte)val;   }
    public CVal( Cell cell ) { Val = (byte)cell; }
    public CVal( Cell cell, int info ) { Val = (byte)( (byte)cell | (info & 0x0f) ); }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Pone/obtiene información que define el tipo de celda
    public Cell Tipo
      {
      get { return (Cell)(Val & 0xF0); }
      set { Val = (byte)value; }
      }
    
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Pone/obtiene información adicional asociado a un tipo de celda
    public int Info
      {
      get { return (Val & 0x0F); }
      set { Val = (byte)((Val & 0xf0) | (value & 0x0f) ); }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Determina si la celda clasifica en alguno de los grupos fundamentales, Piso, Pared o Bloque
    internal bool IsPiso()   { return Val<(byte)Cell.Bloque; }
    internal bool IsPared()  { return Val>=(byte)Cell.Pared && Val<(byte)Cell.Pusher; }
    internal bool IsBloque() { return Val>=(byte)Cell.Bloque && Val<(byte)Cell.Pared; }
    internal bool IsPusher() { return Val==(byte)Cell.Pusher; }
    internal bool IsTarget() { return Val==(byte)Cell.Target; }

    public override string ToString()
      {
	    return Val.ToString("X2");
      }

    } //=============================================================================================================================================================

  //==============================================================================================================================================================
  // Clase que guarda y maneja los datos de un bloque
  public class BlockData
    {
    public Cell Tipo;                                                 // Tipo de bloque  (por ahora solo soporta uno)

    public int Row;                                                   // Fila donde esta el bloque
    public int Col;                                                   // Columna donde esta el bloque

    public int Id;                                                    // Identificador del bloque (debe corresponder con el del target donde se puede ubicar)
    public CVal On;                                                   // Contenido que tenia la celda sobre la que esta el bloque

    public CtrlBlock Ctrl;                                            // Control para representar el bloque en pantalla
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public int ID {                                                   // Obtiene/Pone el identificador del bloque
                  get{ return Id;} 
                  set{ Id = value; Ctrl.Info.Text = value.ToString(); } 
                  }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    string sImage = null;
    public string Image {                                             // Obtiene/Pone imagen que representa al bloque, por el nombre del fichero
                        get { return sImage; } 
                        set {
                            sImage = value;                           // Guarda el nombre

                            Ctrl.Img.Source = Escena.CreateBitmap(value);   // Carga la imagen desde un fichero

                            if( Ctrl.Img.Source == null )             // Si no se pudo cargar la imagen
                              Ctrl.Frame.Background = Ctrl.bckgd;     // Pone un rectangulo en representación
                            else                                      // Si se cargo
                              Ctrl.Frame.Background = null;           // Quita el rectangulo
                            } 
                        }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Crea un bloque con todos sus datos
    public BlockData( int col, int row, int Id, string Img=null  )
      { 
      this.Row  = row ;
      this.Col  = col ;

      this.On   = Escena.GetCell( col, row ); 
      this.Ctrl = new CtrlBlock( col, row );
      this.Tipo = Cell.Bloque;
      this.Id   = 0;

      Image = Img ;
      ID    = Id;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Mueve el bloque de una posición a otra
    public void Move( int col, int row  )
      { 
      var Val = Escena.GetCell( Col, Row );                           // Información del bloque de la celda donde esta
      var tmp = Escena.GetCell( col, row );                           // Guarda contenido de la celda hacia donde se va a mover

      Escena.SetCell( Col, Row, On  );                                // Restaura el contenido de la celda donde esta
      Escena.SetCell( col, row, Val );                                // Actualiza la celda hacia donde va, con información del bloque

      On  = tmp;                                                      // Guarda contenido de la celda sobre la que esta el bloque
      Row = row ;                                                     // Actualiza fila 
      Col = col ;                                                     // Actualiza columna

      Canvas.SetLeft( Ctrl, Escena.GetX(col) );                       // Mueve el control a la nueva posicion
      Canvas.SetTop ( Ctrl, Escena.GetY(row) );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Actualiza la posición del bloque en la pantalla
    public void UpdatePos()
      { 
      Canvas.SetLeft( Ctrl, Escena.GetX(Col) );                       // Mueve el control a la nueva posicion
      Canvas.SetTop ( Ctrl, Escena.GetY(Row) );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene los datos de un bloque, desde una cadena de caracteres
    public static BlockData FromString( string sData )
      { 
      var tokens = sData.Split(',');                                  // Divide las subcadenas separadas por ,
      if( tokens.Length < 5 ) return null;                            // Si no tiene 5 elementos, la cadena no es valida

      try 
	      {	        
		    int tipo = int.Parse( tokens[0] );                            // Primer elemnto como tipo del bloque
		    int col  = int.Parse( tokens[1] );                            // Segundo elemnto columna donde esta el bloque
		    int row  = int.Parse( tokens[2] );                            // Tercera elemento columna fila donde esta el bloque
		    int Id   = int.Parse( tokens[3] );                            // Cuarto elemento identificador del bloque

        string sImg = Escena.GetEscenaPath( tokens[4] );              // Quito elemento fichero de la imagen que representa al bloque

        return new BlockData( col, row, Id, sImg  );                  // Crea un objeto con todos los datos
	      }
	    catch {  return null; }                                         // Hubo algún error al convertir los valores
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene los datos del bloque en forma de cadena de caracteres
    public override string ToString()
      {
 	    return ((byte)Tipo).ToString() + ',' + Col + ',' + Row + ',' + Id + ',' + Escena.GetName( Image );
      }
    }

  //==============================================================================================================================================================
  // Define todos los parametros de la escena
  public static class Escena
    {
    static int nCols = 12;                                            // Número de columnas de la escena
    static int nRows = 8 ;                                            // Número de filas de la escena
    static int cSize = 80;

    static int xIni = 0;                                              // Desplazaminto de la escena respecto a origen su contenedor
    static int yIni = 0;

    static public bool ShowGrid   = true;                             // Define elementos de la escena que pueden ser visibles o no
    static public bool ShowID     = true;
    static public bool ShowPared  = true;
    static public bool ShowTarget = true;
    static public bool ShowFondo  = true;

    static public string sImgFondo  = "";                             // Nombre del fichero de la imagen de fondo si la hay
    static public string sFillFondo = "";                             // Tipo de rellenado del fondo de la escena que excede el tamaño de la imagen de fondo
    static public Canvas GameZone;                                    // Elemento de la inteface donde se dibuja la escena

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    
    static public List<BlockData> Blocks = new List<BlockData>();     // Listado de los bloque que conforman la escena

    static CVal[,] Grid = new CVal[ 20, 20 ];                         // Arreglo donde se guarda la información de cada celda

    static public CtrlPush Pusher;                                    // Objeto que representa al pusher

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Propiedades fundamentales de la escena
    public static int zCell   { get{ return cSize;} set{ cSize = value;} }
    public static int OffSetX { get{ return  xIni;} set{ xIni  = value;} }
    public static int OffSetY { get{ return  yIni;} set{ yIni  = value;} }
    public static int Width   { get{ return  cSize*nCols;} }
    public static int Height  { get{ return  cSize*nRows;} }
    public static int Cols    { get{ return  nCols;} }
    public static int Rows    { get{ return  nRows;} }
        
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Funciones para conversión de coordenadaa
    public static int GetX( int col ) { return xIni + col * cSize;}                   // Posicion horizontal en pixeles de la colunma 'col'
    public static int GetY( int row ) { return yIni + row * cSize;}                   // Posicion vertical en pixeles de la fila 'row' 
    public static int GetXM( int col ) { return xIni + col * cSize + cSize/2;}        // Posicion media horizontal en pixeles de la colunma 'col'
    public static int GetYM( int row ) { return yIni + row * cSize + cSize/2;}        // Posicion media vertical en pixeles de la fila 'row' 
    public static int DistX( int cols ) { return cols * cSize;}                       // Distancia en pixeles que ocupan una cantidad de columnas
    public static int DistY( int rows ) { return rows * cSize;}                       // Distancia en pixeles que ocupan una cantidad de filas
    public static int GetCol( double x ) { return (int)((x - xIni) / cSize); }        // Columna de de la posición x en pixeles
    public static int GetRow( double y ) { return (int)((y - yIni) / cSize); }        // Fila de de la posición y en pixeles
    public static int DistCol( double xDist ) { return (int)(xDist/cSize); }          // Número de columnas de una cantidad de pixeles
    public static int DistRow( double yDist ) { return (int)(yDist/cSize); }          // Número de filas de una cantidad de pixeles

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Calcula los desplazamientos de la escena según sus dimensiones y el número de filas o de columnas
    public static void CalOffsetX( double w ) { xIni = (int)((w - (nCols*cSize))/2.0); }  
    public static void CalOffsetY( double h ) { yIni = (int)((h - (nRows*cSize))/2.0); }  

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Inicializa una escena con todos sus parametros principales
    public static void Init( int cSize, int Cols, int Rows, Canvas Zone=null ) 
      { 
      if( Zone != null ) GameZone = Zone;

      nCols = Cols;
      nRows = Rows;
      zCell = cSize;

      if( GameZone != null )
        {
        CalOffsetX( GameZone.Width  );
        CalOffsetY( GameZone.Height );
        }

      // Pone piso en todas las celdas
      for( int row=0; row<Rows; ++row )                               // Recorre todas las filas  
        for( int col=0; col<Cols; ++col )                             // Recorre todas las columnas
          Grid[ row, col ] = new CVal(Cell.Piso);                     // Pone piso  

      Blocks.Clear(); 

      if( Pusher != null )
        Pusher.Move( nCols/2, nRows/2, false );
      }  

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Asocia un objeto pusher de la interface de usuario, con la escena
    public static void SetPusher( CtrlPush Push ) 
      { 
      Pusher = Push;
      Pusher.Move( Cols/2, Rows/2 );

      if( LastFilePath == null )
        LastFilePath = Push.NameBase;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Mueve el pusher a la posicion col,row dentro de la escena
    public static void MovePusher( int col, int row ) 
      { 
      Pusher.Move( col, row );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Define si usa el cursor, de la mano para indicar movimiento o no
    public static void ShowCursor( bool show ) 
      { 
      Pusher.Cursor      = (show)? Cursors.Hand : Cursors.Arrow;
      Pusher.ForceCursor = show;

      for( int i=0; i<Blocks.Count; i++)
        { 
        Blocks[i].Ctrl.Cursor      = (show)? Cursors.Hand : Cursors.Arrow;
        Blocks[i].Ctrl.ForceCursor = show;
        }
      }

    ////----------------------------------------------------------------------------------------------------------------------------------------------------------
    //public static void RefeshPusher( Image img ) 
    //  { 
    //  Pusher.Refresh( img );
    //  }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public static BlockData AddBlock( int col, int row, int Id, string Img=null ) 
      { 
      if( Img == null ) Img = FindImage( Id );
      var bck = new BlockData( col, row, Id, Img );

      Blocks.Add( bck );

      Grid[row, col] = new CVal( Cell.Bloque, Blocks.Count-1 );

      return bck; 
      }  

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra el bloque que esta en la posición col,row
    public static BlockData DelBlock( int col, int row )
      {
      var Val = Grid[row, col];                                       // Obtiene contenido de la celda  
      int idx = Val.Info;                                             // Obtiene indice del bloque

      var block = Blocks[idx];                                        // Obtiene el bloque del arreglo de bloques

      Blocks.RemoveAt(idx);                                           // Borra el bloque del arreglo

      Grid[row, col] = block.On;                                      // Restaura el contenido de la celda

      // Actualiza el indice de todos los bloques en la escena del borrado hacia adelante
      for( int i=idx; i<Blocks.Count; ++i )                           
        {
        var xCel = Blocks[i].Col;                                     // Obtiene la celda
        var yCel = Blocks[i].Row;

        Grid[yCel, xCel] =  new CVal( Cell.Bloque, i );               // Actualiza el indice en la celda
        }

      return block;                                                   // Retorna bloque borrado
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Quita todos los bloques de la escena
    public static void ClearBlocks()
      {
      foreach( var bck in Blocks )                                    // Recorre todos los bloques
        Grid[bck.Row, bck.Col] = bck.On;                              // Restaura el contenido de la celda sobre la que esta

      Blocks.Clear();                                                 // Borra la lista completa
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Chequea si todos los bloques de la escena estan en la posición correcta
    public static bool CheckBlockPos()
      {
      if( Blocks.Count==0 ) return false;                             // No hay bloque para posicionar

      foreach( var bck in Blocks )                                    // Recorre todos los bloques
        { 
        if( bck.Id >=12 ) continue;                                   // No tiene en cuanta los bloques con id mayor a 14
        if( bck.On.Tipo != Cell.Target || bck.On.Info != bck.Id )     // Si no esta sobre un target o el Id es distinto al del target
          return false;                                               // La escena no esta resuelta
        }

      return true;                                                    // Todos los bloques estan en posición correcta
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Busca si algun bloque del mismo tipo, tiene una imagen
    public static string FindImage(int ID)
      {
      foreach( var bck in Blocks )                                    // Recorre todos los bloques
        if( bck.Image!=null && bck.ID==ID )                           // El bloque tiene imagen y es del identificado requerido
          return bck.Image;                                           // Retorna el nombre de la imagen

      return null;                                                    // Retorn nulo (No encontro imagen)
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Funciones para recuperar/poner el contenido de una celda
    public static CVal GetCell( int col, int row           ) { return Grid[row, col]; }
    public static void SetCell( int col, int row, CVal Val ) { Grid[row, col] = Val; }
    public static void SetCell( int col, int row, Cell Val ) { Grid[row, col] = new CVal(Val); }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public static string LastFilePath = null;                           // Camino de la ultima escena cargada o que se va a cargar/salvar

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene el camino completo del fichero 'FName' respecto a la ultima escena manipulada
    public static string GetEscenaPath( string FName, bool chgRoot = true )
      { 
      if( string.IsNullOrWhiteSpace(FName) ) return null;

      if( !chgRoot && Path.IsPathRooted(FName) ) return FName;

      var FPath = Path.GetDirectoryName( LastFilePath );
      return FPath + '\\' + Path.GetFileName( FName );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene solo el nombre del fichero del camino FPath
    public static string GetName( string FPath )
      { 
      return Path.GetFileName( FPath );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene el nombre de la imagen de fondo normalizada, de forma tal que coincide con el de la escena y solo cambia la extención
    public static string GetStdImgFondo()
      { 
      if( Escena.sImgFondo == null ) return "";

      return Path.GetFileNameWithoutExtension(LastFilePath) + Path.GetExtension( sImgFondo );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public static BitmapImage CreateBitmap( string FName )
      {
      try
        {
        BitmapImage bi = new BitmapImage();

        bi.BeginInit();

        bi.CacheOption   = BitmapCacheOption.None;
        bi.CreateOptions = BitmapCreateOptions.None;
        bi.UriSource = new Uri( FName, UriKind.RelativeOrAbsolute );

        bi.EndInit();

        return bi;
        }
      catch { return null; }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    const string DocType = "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n";
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Guarda los datos de la escena en formato XML, si 'snapshot' es verdadero no se tienen en cuenta los datos de la solucion
    internal static string ToXml( bool snapshot )
      {
      string sXml ="<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + DocType +
                    "<plist version=\"1.0\">\n" +
                    "<dict>\n" +
	                  "  <key>CellSize</key>\n" +
	                  "  <integer>" + Escena.zCell + "</integer>\n" +
	                  "  <key>Width</key>\n" +
	                  "  <integer>" + GameZone.Width + "</integer>\n" +
	                  "  <key>Height</key>\n" +
	                  "  <integer>" + GameZone.Height + "</integer>\n" +
	                  "  <key>ImgFill</key>\n" +
	                  "  <string>" + sFillFondo +"</string>\n" +
	                  "  <key>ImgFondo</key>\n" +
	                  "  <string>" + (snapshot? GetName(sImgFondo) : GetStdImgFondo()) +"</string>\n" + 
	                  "  <key>Grid</key>\n" +
	                  "  <array>\n" +
		                      GridToStr() +
	                  "  </array>\n" +
	                  "  <key>Blocks</key>\n" +
	                  "  <array>\n" +
		                      BlocksToStr() +
	                  "  </array>\n" +
	                  "  <key>Pusher</key>\n" +
	                  "  <string>" + Pusher + "</string>\n"; 

                  if( !snapshot )
                    {
	          sXml += "  <key>Moves</key>\n" +
                    "  <integer>" + AnimatePath.GetSolutionMoves() + "</integer>\n" +
                    "  <key>Solution</key>\n" +
	                  "  <array>\n" +
		                      SolutionToStr() +
	                  "  </array>\n";
                    }

            sXml += "</dict>\n" +
                    "</plist>";

      return sXml;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene una representación de la solución en forma de cadena de caracteres
    private static string SolutionToStr()
      {
      var Lst = AnimatePath.Solution;
      if( Lst==null ) return "";

      var sData = new StringBuilder( 30 * Lst.Count ); 
      for( int i=0; i<Lst.Count; ++i )                               
        {
        sData.Append( "    <string>" );                             // Adiciona Tag inicial de la fila
        sData.Append( Lst[i].ToString() );
        sData.Append( "</string>\n" );                              // Adiciona Tag final de la fila
        }

      return sData.ToString();                                      // Retorna la cadena de texto
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene una representación de la regilla en forma de cadena de caracteres
    private static string GridToStr()
      {
      var sMatrix = new StringBuilder( Rows * (3*Cols + 24) ); 
      for( int row=0; row<Rows; ++row )                               // Recorre todas las filas  
        {
        sMatrix.Append( "    <string>" );                             // Adiciona Tag inicial de la fila

        for( int col=0; col<Cols; ++col )                             // Recorre todas las columnas
          {
          if( col>0 ) sMatrix.Append(',');                            // Si no es la primera columna, adiciona un separador

          var Val = Grid[ row, col ];                                 // Obtiene el contenido de la celda

          if( Val.Tipo == Cell.Bloque ) Val = Blocks[Val.Info].On;    // Si contiene un bloque, guarda la información inicial
          if( Val.Tipo == Cell.Pusher ) Val = Pusher.On;              // Si es el pusher, guarda la información inicial

          sMatrix.Append( Val.ToString()  );                          // Adiciona valor de la celda como cadena
          }

        sMatrix.Append( "</string>\n" );                              // Adiciona Tag final de la fila
        }

      return sMatrix.ToString();                                      // Retorna la cadena de texto
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Obtiene una representación de todos los bloques de la escena en forma de cadena de caracteres y guarda las imagenes asociadas a los bloques
    private static string BlocksToStr()
      {
      var sBlocks = new StringBuilder( 20 * Blocks.Count  ); 
      for( int i=0; i<Blocks.Count; ++i )                              // Recorre todos los bloques
        {
        var bck = Blocks[i];                                           // Bloque actual

        sBlocks.Append( "    <string>" );                              // Adiciona el tag inicial
        sBlocks.Append( bck.ToString() );                              // Convierte bloque a cadena y lo agrega
        sBlocks.Append( "</string>\n" );                               // Adiciona el tag final
        }

      return sBlocks.ToString();                                       // Retorna la cadena
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Hace una copia de todas las imagenes de la escena hacia el directorio definido por 'LastFilePath'
    public static void CopyImgs()
      {
      var CpyLst = new Dictionary<string,string>();                   // Lista de todas las imagenes que hay que copiar

      if( Escena.sImgFondo != null )                                  // Si hay imagen de fondo
        CpyLst[Escena.sImgFondo] = GetEscenaPath(GetStdImgFondo());   // La agrega en la lista

      //  CpyLst[Escena.sImgFondo] = GetEscenaPath(Escena.sImgFondo);   // La agrega en la lista

      for( int i=0; i<Blocks.Count; ++i )                             // Recorre todos los bloques
        {
        var bck = Blocks[i];                                          // Bloque actual

        if( bck.Image != null  )                                      // Tiene imagen
          CpyLst[bck.Image] = GetEscenaPath( bck.Image );             // La agrega en la lista
        }

      var Name = Pusher.NameBase;
      CpyLst[Name] = Escena.GetEscenaPath(Name);                      // Guarda imagen principal del pusher

      for( int i=1; i<Pusher.BitMaps.Count; ++i )                     // Recorre todas las imagenes de movimiento
        {
        var oldPath = Name.Replace( ".png", i + ".png" );             // Obtiene camino original

        CpyLst[oldPath] = Escena.GetEscenaPath(oldPath);              // Guarda la imagen en la lista
        }

      foreach( var file in CpyLst )                                   // Recorre toda la lista
        {
        try{ File.Copy( file.Key, file.Value, true );  }              // Intenta copia la imagen
        catch {}                                                      // Si error al guardar lo ignora
        }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    public static bool FromXml( string sXml )
      {
      sImgFondo = null;

      try
        {
        sXml = sXml.Replace( DocType, "" );

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml( sXml );

        Blocks.Clear();
        XmlNode root = xmlDoc.FirstChild;

	      var nodes = root.NextSibling.SelectNodes("dict/key");
	      if( nodes.Count < 2 ) return false;

        foreach( XmlNode key in nodes)
          {
          var sKey  = key.InnerText;
          var nData = key.NextSibling;

          if( sKey=="ImgFondo" ) sImgFondo  = GetEscenaPath( nData.InnerText );
          if( sKey=="ImgFill"  ) sFillFondo = nData.InnerText;

          if( sKey=="CellSize" && !GetIntData  ( nData, 0 ) ) return false;
          if( sKey=="Width"    && !GetIntData  ( nData, 1 ) ) return false;
          if( sKey=="Height"   && !GetIntData  ( nData, 2 ) ) return false;
          if( sKey=="Grid"     && !GetCellsGrid( nData    ) ) return false;
          if( sKey=="Blocks"   && !GetBlocks   ( nData    ) ) return false;
          if( sKey=="Pusher"   && !GetPusher   ( nData    ) ) return false;
          if( sKey=="Solution" && !GetSolution ( nData    ) ) return false;
          }

        return true;
        }
      catch { return false; }
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private static bool GetPusher(XmlNode node)
      {
      if( !CtrlPush.FromString( node.InnerText ) ) return false;

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private static bool GetBlocks(XmlNode node)
      {
    	var bcks = node.SelectNodes("string");

      for( int i=0; i<bcks.Count; ++i )
        {
        var bck = BlockData.FromString( bcks[i].InnerText );
        if( bck == null ) return false;

        Blocks.Add( bck );
        }

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private static bool GetIntData( XmlNode node, int iTipo )
      {
      int Val;
      var ret = int.TryParse( node.InnerText, out Val );
      if( iTipo == 0 ) zCell = Val;
      if( iTipo == 1 && GameZone!=null ) GameZone.Width  = Val;
      if( iTipo == 2 && GameZone!=null ) GameZone.Height = Val;

      return ret;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    private static bool GetCellsGrid(XmlNode node )
      {
    	var rows = node.SelectNodes("string");
      if( rows.Count < 3 ) return false;

      var cols = rows[0].InnerText.Split(',');
      if( cols.Length < 3 ) return false;

      Init( zCell, cols.Length, rows.Count, null );

      for( int row=0; row<rows.Count; ++row )
        {
        cols = rows[row].InnerText.Split(',');

        for( int col = 0; col<cols.Length && col<Cols; col++ )
          {
          byte val;
          if( byte.TryParse( cols[col], NumberStyles.HexNumber, null, out val ) )
            SetCell( col, row, new CVal(val) );
          else
            return false;
          }
        }

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Lee desde el nodo 'node' lo datos correspondiente a la solución de la escena
    private static bool GetSolution(XmlNode node)
      {
      AnimatePath.Solution = null;                                              // Asume por defecto que no tiene solución

    	var Pnts = node.SelectNodes("string");                                    // Lee todos los nodos que definen los puntos

      var tmp = new List<PathPnt>();                                            // Crea una lista de puntos temporal, vacia

      int lstCol=0, lstRow=0;
      for( int i=0; i<Pnts.Count; ++i )                                         // Recorre todos los puntos
        {
        var pnt = PathPnt.FromString( Pnts[i].InnerText );                      // Obtiene el texto con los datos y lo covierte a punto
        if( pnt.Sgn == 0 ) return false;                                        // Es punto no se inicializo bien, retorna error

        if( i==0 )                                                              // Si es el primer punto
          {
          pnt.Sent = SegmSent.Indef;                                            // Sentido indefinido
          pnt.Sgn  = 0;                                                         // Signo positivo
          }
        else                                                                    // Otro punto, diferente al inicial
          {
          if( lstRow == pnt.Row )                                               // Esta en la misma columna del punto anterior
            {
            pnt.Sent = SegmSent.Horz;                                           // Sentido horizontal
            pnt.Sgn  = (lstCol<pnt.Col)? 1 : -1;                                // Hacia la derecha positivo, al izquierda negativo
            }
          else                                                                  // Asume que debe estar en la misma fila
            {
            pnt.Sent = SegmSent.Vert;                                           // Sentido vertical
            pnt.Sgn  = (lstRow<pnt.Row)? 1 : -1;                                // Hacia abajo positivo, hacia arriba negativo
            }
          }

        tmp.Add( pnt );                                                         // Adiciona punto a la lista

        lstCol = pnt.Col;                                                       // Almacena la ultima fila/columna analizada
        lstRow = pnt.Row;
        }

      if( tmp.Count>0 ) AnimatePath.Solution = tmp;                             // Si se pudo leer mas de un punto, se toma lista como solución

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Recorta la escena eliminando todas las filas y todas las columnas que sobran
    public static bool Recorta()
      { 
      int newRows = nRows;
      int newCols = Cols;
 
      for( int row=nRows-1; row>0; --row )
        {
        int col=nCols-1;
        for( ;col>0; --col )              
          if( !GetCell(col,row).IsPared()  ) break;

        if( col>0 )
          { 
          newRows = row + 1;
          break;
          }
        }

      for( int col=nCols-1; col>0; --col )
        {
        int row=nRows-1;
        for( ;row>0; --row )              
          if( !GetCell(col,row).IsPared()  ) break;

        if( row>0 )
          { 
          newCols = col + 1;
          break;
          }
        }

      if( newRows != nRows || newCols != nCols )
        { 
        nRows = newRows;
        nCols = newCols;

        if( GameZone != null )
          {
          CalOffsetX( GameZone.Width  );
          CalOffsetY( GameZone.Height );
          }

        return true;
        }

      return false;
      }  

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Intenta adicionar una columna en la escena en la posicion idx
    internal static void AddCol(int idx)
      {
      for( int row=0; row<nRows; ++row )
        if( idx < nCols ) 
          {
          for( int col=nCols-1; col>=idx; --col )
            OffSetCell( col, row, 1, 0 );
          }
        else SetCell( nCols, row, Cell.Piso );                        

      ++nCols;
      if( GameZone != null ) CalOffsetX( GameZone.Width );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Intenta adicionar una fila en la escena en la posicion idx 
    internal static void AddRow(int idx)
      {
      for( int col=0; col<nCols; ++col )
        if( idx < nRows ) 
          {
          for( int row=nRows-1; row>=idx; --row )
            OffSetCell( col, row, 0, 1 );
          }
        else SetCell( col, nRows, Cell.Piso );                        

      ++nRows;
      if( GameZone != null ) CalOffsetY( GameZone.Height );
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra la columna en la escena en la posicion idx 
    internal static bool DelCol( int idx)
      {
      if( idx>=nCols ) idx=nCols-1;
      if( idx<0      ) idx=0;

      for( int row=0; row<nRows; ++row )
        if( GetCell(idx,row).Tipo != Cell.Piso ) return false;

      for( int row=0; row<nRows; ++row )
        for( int col=idx+1; col<nCols; ++col )
          OffSetCell( col, row, -1, 0 );

      --nCols;
      if( GameZone != null ) CalOffsetX( GameZone.Width );

      return true;
      }

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Borra la fila en la escena en la posicion idx 
    internal static bool DelRow(int idx)
      {
      if( idx>=nRows ) idx=nRows-1;
      if( idx<0      ) idx=0;

      for( int col=0; col<nCols; ++col )
        if( GetCell(col,idx).Tipo != Cell.Piso ) return false;

      for( int col=0; col<nCols; ++col )
        for( int row=idx+1; row<nRows; ++row )
          OffSetCell( col, row, 0, -1 );

      --nRows;
      if( GameZone != null ) CalOffsetY( GameZone.Height );

      return true;
      }
    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    // Intenta adicionar una fila en la escena en la posicion idx 
    internal static void OffSetCell(int col, int row, int xOff, int yOff )
      {
      var Val = Grid[row,col];                                    // Toma el valor guardado en la celda

      if( xOff>0 || yOff>0 )                                      // Si se esta adicionando una fila a columna
        SetCell( col, row, Cell.Piso );                           // Pone piso en la celda

      col += xOff;                                                // Corre la celda lo offset dados
      row += yOff;

      SetCell( col, row, Val );                                   // Pone el valor que tenia la celda anteriormente

      if( Val.IsBloque() )                                        // Si habia un bloque en la celda
        { 
        var blq = Blocks[ Val.Info ];                             // Obtiene el bloque a partir de información de la celda

        blq.Col = col;                                            // Actualiza posición del bloque
        blq.Row = row;                                            
        }

      if( Val.IsPusher() )                                        // Si estaba el pusher en la celda
        { 
        Pusher.Col = col;                                         // Actualiza posición del pusher
        Pusher.Row = row;
        }
      }


    } //=============================================================================================================================================================
  }
