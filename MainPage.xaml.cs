using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Xml.Schema;
using System.Xml.Linq;
using C1.Silverlight.DataGrid;
using C1.Silverlight.Data;
using C1.Silverlight;
using System.Windows.Data;
using System.Reflection;
using System.Windows.Media.Imaging;
using C1.Silverlight.Uploader;
using System.IO;
using System.IO.IsolatedStorage;

// --------------
// (c) 2014 DrSdl
// The program is provided as is, without any warranties
// This program is only for educational purposes.
//
// Visual interface to demonstrate core loading and unloading activities
// This tool is mainly used for practical core fuel pattern optimisation activities
// The core loading and the fuel pool configuration is described by an xml-file
// 
// The WebApp is running at www.kewura.de
// --------------
namespace CoreX
{
    public partial class MainPage : UserControl
    {
        private C1Uploader _uploader;
        private FilesPerRequest _mode = FilesPerRequest.OneFilePerRequest;
        private string xmlfile;
        public FilesPerRequest FilesPerRequest
        {
            get { return _mode; }
            set { _mode = value; }
        }
        private int homolog4you;
        private int[,] SourceHomol;
        private int[,] TargetHomol;
        private int[,] hID;
        private int SourceQuad;
        private int TargetQuad;
        List<brennelementresults> MyDatasResult;

        C1DragDropManager _ddManager = new C1DragDropManager();
        Piece[,] _tablePieces;
        Piece[,] _tablePiecesPond;
        Border[,] _tableBorders;
        Border[,] _tableBordersPond;
        ColumnDefinition fd;
        RowDefinition fr;

        public MainPage()
        {
             InitializeComponent();
             xmlfile = "default";
             homolog4you = 0; 
            
             #region init-BE-stuff
             SourceHomol =new int[4,2]; TargetHomol = new int[4, 2];
             SourceQuad = 1; TargetQuad = 1;

             SourceHomol[0, 0] = 0; SourceHomol[1, 0] = 0; SourceHomol[2, 0] = 0; SourceHomol[3, 0] = 0;
             SourceHomol[0, 1] = 0; SourceHomol[1, 1] = 0; SourceHomol[2, 1] = 0; SourceHomol[3, 1] = 0;

             TargetHomol[0, 0] = 0; TargetHomol[1, 0] = 0; TargetHomol[2, 0] = 0; TargetHomol[3, 0] = 0;
             TargetHomol[0, 1] = 0; TargetHomol[1, 1] = 0; TargetHomol[2, 1] = 0; TargetHomol[3, 1] = 0;

             hID = new int[48, 8];

             InitHomologID();
             #endregion init-BE-stuff
            
             LoadMyNewsGrid();
             
              #region point-core-and-pond
              for (int i = 0; i < 15; i++)
              {
                  CoreTable.RowDefinitions.Add(new RowDefinition());
                  CoreTable.ColumnDefinitions.Add(new ColumnDefinition());
              }

              for (int i = 0; i < 4; i++)
              {
                  fd = new ColumnDefinition();
                  fd.Width = new GridLength(50);
                  PondTable.ColumnDefinitions.Add(fd);
              }

              for (int i = 0; i < 100; i++)
              {
                  fr = new RowDefinition();
                  fr.Height = new GridLength(50);
                  PondTable.RowDefinitions.Add(fr);
              }
              #endregion point-core-and-pond

              CreateNewCore();
              CreateNewPond();

                // handle Drag & Drop event
                _ddManager.DragDrop += new DragDropEventHandler(_ddManager_DragDrop);
                _ddManager.DragEnter += new DragDropEventHandler(_ddManager_DragEnter);
                _ddManager.DragLeave += new DragDropEventHandler(_ddManager_DragLeave);
                _ddManager.TargetMarker.Visibility = Visibility.Collapsed;
                _ddManager.SourceMarker.Visibility = Visibility.Collapsed;
                // _ddManager.DragOver += new DragDropEventHandler(_ddManager_DragOver);
           
             CoreControl.SelectedIndex = 1;
            
        }

        void _ddManager_DragEnter(object source, DragDropEventArgs e)
        {
            if (dnbr.IsChecked == true) return;

            var target = (Border)e.DropTarget;
            target.Tag = target.Background;
            target.Background = new SolidColorBrush(Colors.Blue);
        }
        void _ddManager_DragOver(object source, DragDropEventArgs e)
        {
            if (dnbr.IsChecked == true) return;

            var target = (Border)e.DropTarget;
            target.Tag = target.Background;
            target.Background = new SolidColorBrush(Colors.Red);
        }
        void _ddManager_DragLeave(object source, DragDropEventArgs e)
        {
            if (dnbr.IsChecked == true) return;

            var target = (Border)e.DropTarget;
            target.Background = (Brush)target.Tag;
        }
        void _ddManager_DragDrop(object source, DragDropEventArgs e)
        {
            if (dnbr.IsChecked == true) return;

            // get desired location
            Border rect = (Border)e.DropTarget;
            int targetRow = Grid.GetRow(rect);
            int targetColumn = Grid.GetColumn(rect);
            Grid targetGrid = (Grid)rect.Parent;

            Piece pieceS;
            Piece pieceT;

            Piece pieceSt1;
            Piece pieceTt1;

            Piece pieceSt2;
            Piece pieceTt2;

            Piece pieceSt3;
            Piece pieceTt3;

            // undo mouseover
            rect.Background = (Brush)rect.Tag;

            // get old location
            FrameworkElement sourceBorder = (e.DragSource as FrameworkElement).Parent as Border;
            int sourceRow = Grid.GetRow(sourceBorder);
            int sourceColumn = Grid.GetColumn(sourceBorder);
            Grid sourceGrid = (Grid)sourceBorder.Parent;

            // piece
            //
            bool isNotUsedPondCell=true;
            bool isSimpleMovement = true;

            // analyze movement
            #region isNotUsedCell
            /*
            if (isNotUsedCell)
            {
                // simple movement
                isSimpleMovement &= (sourceRow + direction == targetRow)
                                 && (Math.Abs(targetColumn - sourceColumn) == 1);

                // eating pieces
                if (!isSimpleMovement)
                {
                    int offset = Math.Abs(targetColumn - sourceColumn);
                    isEating &= (targetRow - sourceRow == direction * offset)
                             && (offset % 2 == 0);

                    // rival pieces
                    int i = 1;
                    while (isEating && i < offset)
                    {
                        int r = Move(sourceRow, targetRow, i);
                        int c = Move(sourceColumn, targetColumn, i);
                        if (_tablePieces[r, c] != null)
                        {
                            isEating &= (_tablePieces[r, c].Team != piece.Team);
                            eatenPieces.Add(_tablePieces[r, c]);
                        }
                        else
                        {
                            isEating = false;
                        }
                        i += 2;
                    }

                    // using empty cells
                    i = 2;
                    while (isEating && i <= offset)
                    {
                        int r = Move(sourceRow, targetRow, i);
                        int c = Move(sourceColumn, targetColumn, i);
                        isEating &= (_tablePieces[r, c] == null);
                        i += 2;
                    }

                    if (!isEating)
                        eatenPieces.Clear();
                }
            }
            */
            #endregion isNotUsedCell

            #region homolog4you=0
            if (homolog4you == 0)
            {
                if (isSimpleMovement && targetGrid.Name.Contains("CoreTable") == true && sourceGrid.Name.Contains("CoreTable") == true)
                {
                    pieceS = _tablePieces[sourceRow, sourceColumn];
                    pieceT = _tablePieces[targetRow, targetColumn];

                    LocatePiece(pieceS, targetRow, targetColumn);
                    LocatePiece(pieceT, sourceRow, sourceColumn);
                }

                if (isSimpleMovement && targetGrid.Name.Contains("PondTable") == true && sourceGrid.Name.Contains("PondTable") == true)
                {
                    isNotUsedPondCell = (_tablePiecesPond[targetRow, targetColumn] == null);
                    if (isNotUsedPondCell == true) return;

                    Piece pieceS1 = _tablePiecesPond[sourceRow, sourceColumn];
                    Piece pieceT1 = _tablePiecesPond[targetRow, targetColumn];

                    LocatePiecePond(pieceS1, targetRow, targetColumn);
                    LocatePiecePond(pieceT1, sourceRow, sourceColumn);
                }

                if (isSimpleMovement && targetGrid.Name.Contains("PondTable") == true && sourceGrid.Name.Contains("CoreTable") == true)
                {
                    isNotUsedPondCell = (_tablePiecesPond[targetRow, targetColumn] == null);
                    if (isNotUsedPondCell == true) return;
                    Piece pieceS2 = _tablePieces[sourceRow, sourceColumn];
                    Piece pieceT2 = _tablePiecesPond[targetRow, targetColumn];

                    LocatePiecePond(pieceS2, targetRow, targetColumn);
                    LocatePiece(pieceT2, sourceRow, sourceColumn);

                }

                if (isSimpleMovement && targetGrid.Name.Contains("CoreTable") == true && sourceGrid.Name.Contains("PondTable") == true)
                {

                    Piece pieceS3 = _tablePiecesPond[sourceRow, sourceColumn];
                    Piece pieceT3 = _tablePieces[targetRow, targetColumn];

                    LocatePiece(pieceS3, targetRow, targetColumn);
                    LocatePiecePond(pieceT3, sourceRow, sourceColumn);

                }
            }
            #endregion homolog4you=0

            #region homolog4you=1
            if (homolog4you == 1)
            {
                if (targetRow == 7 && targetColumn == 7) return; // central assembly not changeable
                if (sourceRow == 7 && sourceColumn == 7) return; // central assembly not changeable

                if (isSimpleMovement && targetGrid.Name.Contains("CoreTable") == true && sourceGrid.Name.Contains("CoreTable") == true)
                {

                    CalcSourceHomol(sourceRow, sourceColumn);
                    CalcTargetHomol(targetRow, targetColumn);

                    #region SourceQuad == TargetQuad
                    if (SourceQuad == TargetQuad)
                    {
                        // Q1
                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceT = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];

                        LocatePiece(pieceS, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiece(pieceT, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        // Q2
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];

                        LocatePiece(pieceSt1, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiece(pieceTt1, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        // Q3
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];

                        LocatePiece(pieceSt2, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiece(pieceTt2, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        // Q4
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];

                        LocatePiece(pieceSt3, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiece(pieceTt3, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                       
                    }
                    #endregion

                    #region SourceQuad = TargetQuad +1
                    if ( (SourceQuad == 1 && TargetQuad==2) || (SourceQuad == 2 && TargetQuad==3) || (SourceQuad == 3 && TargetQuad==4) || (SourceQuad == 4 && TargetQuad==1) )
                    {

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        pieceT = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];

                        LocatePiece(pieceT, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiece(pieceTt1, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiece(pieceTt2, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiece(pieceTt3, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);

                        LocatePiece(pieceS, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiece(pieceSt1, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiece(pieceSt2, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiece(pieceSt3, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);

                    }
                    #endregion

                    #region SourceQuad = TargetQuad +2
                    if ((SourceQuad == 1 && TargetQuad == 3) || (SourceQuad == 2 && TargetQuad == 4) || (SourceQuad == 3 && TargetQuad == 1) || (SourceQuad == 4 && TargetQuad == 2))
                    {

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        pieceT = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];

                        LocatePiece(pieceS, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiece(pieceSt1, 15 - TargetHomol[3, 1], TargetHomol[3, 0] -1);
                        LocatePiece(pieceSt2, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiece(pieceSt3, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);

                        LocatePiece(pieceT, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiece(pieceTt1, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiece(pieceTt2, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiece(pieceTt3, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        
                    }
                    #endregion

                    #region SourceQuad = TargetQuad +3
                    if ((SourceQuad == 1 && TargetQuad == 4) || (SourceQuad == 2 && TargetQuad == 1) || (SourceQuad == 3 && TargetQuad == 2) || (SourceQuad == 4 && TargetQuad == 3))
                    {

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        pieceT = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];

                        LocatePiece(pieceS, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiece(pieceSt1, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiece(pieceSt2, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiece(pieceSt3, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);

                        LocatePiece(pieceT, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiece(pieceTt1, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiece(pieceTt2, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiece(pieceTt3, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        
                    }
                    #endregion

                }


                if (isSimpleMovement && targetGrid.Name.Contains("CoreTable") == true && sourceGrid.Name.Contains("PondTable") == true)
                {

                    CalcSourceHomolPond(sourceRow, sourceColumn);
                    CalcTargetHomol(targetRow, targetColumn);

                    #region SourceQuad == TargetQuad
                    if (SourceQuad == TargetQuad)
                    {
                        // Q1
                        pieceS = _tablePiecesPond[SourceHomol[0, 0], SourceHomol[0, 1] ];
                        pieceT = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];

                        LocatePiece(pieceS, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiecePond(pieceT, SourceHomol[0, 0], SourceHomol[0, 1]);
                        // Q2
                        pieceSt1 = _tablePiecesPond[SourceHomol[1, 0], SourceHomol[1, 1] ];
                        pieceTt1 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];

                        LocatePiece(pieceSt1, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiecePond(pieceTt1, SourceHomol[1, 0], SourceHomol[1, 1] );
                        // Q3
                        pieceSt2 = _tablePiecesPond[SourceHomol[2, 0], SourceHomol[2, 1] ];
                        pieceTt2 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];

                        LocatePiece(pieceSt2, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiecePond(pieceTt2, SourceHomol[2, 0], SourceHomol[2, 1] );
                        // Q4
                        pieceSt3 = _tablePiecesPond[SourceHomol[3, 0], SourceHomol[3, 1] ];
                        pieceTt3 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];

                        LocatePiece(pieceSt3, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiecePond(pieceTt3, SourceHomol[3, 0], SourceHomol[3, 1] );

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +1
                    if ((SourceQuad == 1 && TargetQuad == 2) || (SourceQuad == 2 && TargetQuad == 3) || (SourceQuad == 3 && TargetQuad == 4) || (SourceQuad == 4 && TargetQuad == 1))
                    {
                       
                        pieceS = _tablePiecesPond[SourceHomol[0, 0], SourceHomol[0, 1]];
                        pieceSt1 = _tablePiecesPond[SourceHomol[1, 0], SourceHomol[1, 1]];
                        pieceSt2 = _tablePiecesPond[SourceHomol[2, 0], SourceHomol[2, 1]];
                        pieceSt3 = _tablePiecesPond[SourceHomol[3, 0], SourceHomol[3, 1]];

                        pieceT = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceS, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiecePond(pieceT, SourceHomol[0, 0], SourceHomol[0, 1]);
                        // Q2
                        LocatePiece(pieceSt1, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiecePond(pieceTt1, SourceHomol[1, 0], SourceHomol[1, 1]);
                        // Q3
                        LocatePiece(pieceSt2, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiecePond(pieceTt2, SourceHomol[2, 0], SourceHomol[2, 1]);
                        // Q4
                        LocatePiece(pieceSt3, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiecePond(pieceTt3, SourceHomol[3, 0], SourceHomol[3, 1]);

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +2
                    if ((SourceQuad == 1 && TargetQuad == 3) || (SourceQuad == 2 && TargetQuad == 4) || (SourceQuad == 3 && TargetQuad == 1) || (SourceQuad == 4 && TargetQuad == 2))
                    {
                        pieceS = _tablePiecesPond[SourceHomol[0, 0], SourceHomol[0, 1]];
                        pieceSt1 = _tablePiecesPond[SourceHomol[1, 0], SourceHomol[1, 1]];
                        pieceSt2 = _tablePiecesPond[SourceHomol[2, 0], SourceHomol[2, 1]];
                        pieceSt3 = _tablePiecesPond[SourceHomol[3, 0], SourceHomol[3, 1]];

                        pieceT = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceS, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiecePond(pieceT, SourceHomol[0, 0], SourceHomol[0, 1]);
                        // Q2
                        LocatePiece(pieceSt1, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiecePond(pieceTt1, SourceHomol[1, 0], SourceHomol[1, 1]);
                        // Q3
                        LocatePiece(pieceSt2, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiecePond(pieceTt2, SourceHomol[2, 0], SourceHomol[2, 1]);
                        // Q4
                        LocatePiece(pieceSt3, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiecePond(pieceTt3, SourceHomol[3, 0], SourceHomol[3, 1]);

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +3
                    if ((SourceQuad == 1 && TargetQuad == 4) || (SourceQuad == 2 && TargetQuad == 1) || (SourceQuad == 3 && TargetQuad == 2) || (SourceQuad == 4 && TargetQuad == 3))
                    {
                        pieceS = _tablePiecesPond[SourceHomol[0, 0], SourceHomol[0, 1]];
                        pieceSt1 = _tablePiecesPond[SourceHomol[1, 0], SourceHomol[1, 1]];
                        pieceSt2 = _tablePiecesPond[SourceHomol[2, 0], SourceHomol[2, 1]];
                        pieceSt3 = _tablePiecesPond[SourceHomol[3, 0], SourceHomol[3, 1]];

                        pieceT = _tablePieces[15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1];
                        pieceTt1 = _tablePieces[15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1];
                        pieceTt2 = _tablePieces[15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1];
                        pieceTt3 = _tablePieces[15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceS, 15 - TargetHomol[3, 1], TargetHomol[3, 0] - 1);
                        LocatePiecePond(pieceT, SourceHomol[0, 0], SourceHomol[0, 1]);
                        // Q2
                        LocatePiece(pieceSt1, 15 - TargetHomol[0, 1], TargetHomol[0, 0] - 1);
                        LocatePiecePond(pieceTt1, SourceHomol[1, 0], SourceHomol[1, 1]);
                        // Q3
                        LocatePiece(pieceSt2, 15 - TargetHomol[1, 1], TargetHomol[1, 0] - 1);
                        LocatePiecePond(pieceTt2, SourceHomol[2, 0], SourceHomol[2, 1]);
                        // Q4
                        LocatePiece(pieceSt3, 15 - TargetHomol[2, 1], TargetHomol[2, 0] - 1);
                        LocatePiecePond(pieceTt3, SourceHomol[3, 0], SourceHomol[3, 1]);

                    }
                    #endregion
                }

                if (isSimpleMovement && targetGrid.Name.Contains("PondTable") == true && sourceGrid.Name.Contains("CoreTable") == true)
                {

                    CalcSourceHomol(sourceRow, sourceColumn);
                    CalcTargetHomolPond(targetRow, targetColumn);

                    #region SourceQuad == TargetQuad
                    if (SourceQuad == TargetQuad)
                    {
                        pieceT   = _tablePiecesPond[TargetHomol[0, 0], TargetHomol[0, 1]];
                        pieceTt1 = _tablePiecesPond[TargetHomol[1, 0], TargetHomol[1, 1]];
                        pieceTt2 = _tablePiecesPond[TargetHomol[2, 0], TargetHomol[2, 1]];
                        pieceTt3 = _tablePiecesPond[TargetHomol[3, 0], TargetHomol[3, 1]];

                        pieceS   = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceT, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiecePond(pieceS, TargetHomol[0, 0], TargetHomol[0, 1]);

                        // Q2
                        LocatePiece(pieceTt1, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiecePond(pieceSt1, TargetHomol[1, 0], TargetHomol[1, 1]);

                        // Q3
                        LocatePiece(pieceTt2, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiecePond(pieceSt2, TargetHomol[2, 0], TargetHomol[2, 1]);

                        // Q4
                        LocatePiece(pieceTt3, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        LocatePiecePond(pieceSt3, TargetHomol[3, 0], TargetHomol[3, 1]);

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +1
                    if ((SourceQuad == 1 && TargetQuad == 2) || (SourceQuad == 2 && TargetQuad == 3) || (SourceQuad == 3 && TargetQuad == 4) || (SourceQuad == 4 && TargetQuad == 1))
                    {
                        pieceT = _tablePiecesPond[TargetHomol[0, 0], TargetHomol[0, 1]];
                        pieceTt1 = _tablePiecesPond[TargetHomol[1, 0], TargetHomol[1, 1]];
                        pieceTt2 = _tablePiecesPond[TargetHomol[2, 0], TargetHomol[2, 1]];
                        pieceTt3 = _tablePiecesPond[TargetHomol[3, 0], TargetHomol[3, 1]];

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceT, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiecePond(pieceS, TargetHomol[0, 0], TargetHomol[0, 1]);

                        // Q2
                        LocatePiece(pieceTt1, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiecePond(pieceSt1, TargetHomol[1, 0], TargetHomol[1, 1]);

                        // Q3
                        LocatePiece(pieceTt2, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        LocatePiecePond(pieceSt2, TargetHomol[2, 0], TargetHomol[2, 1]);

                        // Q4
                        LocatePiece(pieceTt3, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiecePond(pieceSt3, TargetHomol[3, 0], TargetHomol[3, 1]);

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +2
                    if ((SourceQuad == 1 && TargetQuad == 3) || (SourceQuad == 2 && TargetQuad == 4) || (SourceQuad == 3 && TargetQuad == 1) || (SourceQuad == 4 && TargetQuad == 2))
                    {
                        pieceT = _tablePiecesPond[TargetHomol[0, 0], TargetHomol[0, 1]];
                        pieceTt1 = _tablePiecesPond[TargetHomol[1, 0], TargetHomol[1, 1]];
                        pieceTt2 = _tablePiecesPond[TargetHomol[2, 0], TargetHomol[2, 1]];
                        pieceTt3 = _tablePiecesPond[TargetHomol[3, 0], TargetHomol[3, 1]];

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceT, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiecePond(pieceS, TargetHomol[0, 0], TargetHomol[0, 1]);

                        // Q2
                        LocatePiece(pieceTt1, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        LocatePiecePond(pieceSt1, TargetHomol[1, 0], TargetHomol[1, 1]);

                        // Q3
                        LocatePiece(pieceTt2, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiecePond(pieceSt2, TargetHomol[2, 0], TargetHomol[2, 1]);

                        // Q4
                        LocatePiece(pieceTt3, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiecePond(pieceSt3, TargetHomol[3, 0], TargetHomol[3, 1]);

                    }
                    #endregion

                    #region SourceQuad == TargetQuad +3
                    if ((SourceQuad == 1 && TargetQuad == 4) || (SourceQuad == 2 && TargetQuad == 1) || (SourceQuad == 3 && TargetQuad == 2) || (SourceQuad == 4 && TargetQuad == 3))
                    {
                        pieceT = _tablePiecesPond[TargetHomol[0, 0], TargetHomol[0, 1]];
                        pieceTt1 = _tablePiecesPond[TargetHomol[1, 0], TargetHomol[1, 1]];
                        pieceTt2 = _tablePiecesPond[TargetHomol[2, 0], TargetHomol[2, 1]];
                        pieceTt3 = _tablePiecesPond[TargetHomol[3, 0], TargetHomol[3, 1]];

                        pieceS = _tablePieces[15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1];
                        pieceSt1 = _tablePieces[15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1];
                        pieceSt2 = _tablePieces[15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1];
                        pieceSt3 = _tablePieces[15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1];

                        // Q1
                        LocatePiece(pieceT, 15 - SourceHomol[3, 1], SourceHomol[3, 0] - 1);
                        LocatePiecePond(pieceS, TargetHomol[0, 0], TargetHomol[0, 1]);

                        // Q2
                        LocatePiece(pieceTt1, 15 - SourceHomol[0, 1], SourceHomol[0, 0] - 1);
                        LocatePiecePond(pieceSt1, TargetHomol[1, 0], TargetHomol[1, 1]);

                        // Q3
                        LocatePiece(pieceTt2, 15 - SourceHomol[1, 1], SourceHomol[1, 0] - 1);
                        LocatePiecePond(pieceSt2, TargetHomol[2, 0], TargetHomol[2, 1]);

                        // Q4
                        LocatePiece(pieceTt3, 15 - SourceHomol[2, 1], SourceHomol[2, 0] - 1);
                        LocatePiecePond(pieceSt3, TargetHomol[3, 0], TargetHomol[3, 1]);

                    }
                    #endregion

                }

            }
            #endregion homolog4you=1
        }

        // Get numerical coordinates of source fuel assemblies which are homologous
        private void CalcSourceHomol(int sr, int sc)
        {
            int cx;
            int cy;

            cx = sc + 1;cy = 15 - sr;

            for (int j = 0; j < 48; j++)
            {
                if ((cx == hID[j, 0] && cy == hID[j, 1]) || (cx == hID[j, 2] && cy == hID[j, 3]) || (cx == hID[j, 4] && cy == hID[j, 5]) || (cx == hID[j, 6] && cy == hID[j, 7]))
                {
                    SourceHomol[0, 0] = hID[j, 0]; SourceHomol[0, 1] = hID[j, 1];
                    SourceHomol[1, 0] = hID[j, 2]; SourceHomol[1, 1] = hID[j, 3];
                    SourceHomol[2, 0] = hID[j, 4]; SourceHomol[2, 1] = hID[j, 5];
                    SourceHomol[3, 0] = hID[j, 6]; SourceHomol[3, 1] = hID[j, 7];

                    if (cx == hID[j, 0] && cy == hID[j, 1]) SourceQuad = 1;
                    if (cx == hID[j, 2] && cy == hID[j, 3]) SourceQuad = 2;
                    if (cx == hID[j, 4] && cy == hID[j, 5]) SourceQuad = 3;
                    if (cx == hID[j, 6] && cy == hID[j, 7]) SourceQuad = 4;
                }
            }
        }
        // Get numerical coordinates of target fuel assemblies which are homologous
        private void CalcTargetHomol(int sr, int sc)
        {
            int cx;
            int cy;

           cx = sc + 1; cy = 15 - sr;
            // cx = sc + 0; cy = 16 - sr;

            for (int j = 0; j < 48; j++)
            {
                if ((cx == hID[j, 0] && cy == hID[j, 1]) || (cx == hID[j, 2] && cy == hID[j, 3]) || (cx == hID[j, 4] && cy == hID[j, 5]) || (cx == hID[j, 6] && cy == hID[j, 7]))
                {
                    TargetHomol[0, 0] = hID[j, 0]; TargetHomol[0, 1] = hID[j, 1];
                    TargetHomol[1, 0] = hID[j, 2]; TargetHomol[1, 1] = hID[j, 3];
                    TargetHomol[2, 0] = hID[j, 4]; TargetHomol[2, 1] = hID[j, 5];
                    TargetHomol[3, 0] = hID[j, 6]; TargetHomol[3, 1] = hID[j, 7];

                    if (cx == hID[j, 0] && cy == hID[j, 1]) TargetQuad = 1;
                    if (cx == hID[j, 2] && cy == hID[j, 3]) TargetQuad = 2;
                    if (cx == hID[j, 4] && cy == hID[j, 5]) TargetQuad = 3;
                    if (cx == hID[j, 6] && cy == hID[j, 7]) TargetQuad = 4;
                }
            }
        }
        // Array initialisation to group homologous groups of assemblies in 15x15 core
        private void InitHomologID()
        {
            hID[0, 0] = 9;   hID[0, 1] = 1;  hID[0, 2] = 15; hID[0, 3] = 9;  hID[0, 4] = 7; hID[0, 5] = 15; hID[0, 6] = 1; hID[0, 7] = 7;
            hID[1, 0] = 10;  hID[1, 1] = 1;  hID[1, 2] = 15; hID[1, 3] = 10; hID[1, 4] = 6; hID[1, 5] = 15; hID[1, 6] = 1; hID[1, 7] = 6;
            hID[2, 0] = 11;  hID[2, 1] = 1;  hID[2, 2] = 15; hID[2, 3] = 11; hID[2, 4] = 5; hID[2, 5] = 15; hID[2, 6] = 1; hID[2, 7] = 5;

            hID[3, 0] = 9;   hID[3, 1] = 2;  hID[3, 2] = 14; hID[3, 3] = 9;  hID[3, 4] = 7; hID[3, 5] = 14; hID[3, 6] = 2; hID[3, 7] = 7;
            hID[4, 0] = 10;  hID[4, 1] = 2;  hID[4, 2] = 14; hID[4, 3] = 10; hID[4, 4] = 6; hID[4, 5] = 14; hID[4, 6] = 2; hID[4, 7] = 6;
            hID[5, 0] = 11;  hID[5, 1] = 2;  hID[5, 2] = 14; hID[5, 3] = 11; hID[5, 4] = 5; hID[5, 5] = 14; hID[5, 6] = 2; hID[5, 7] = 5;
            hID[6, 0] = 12;  hID[6, 1] = 2;  hID[6, 2] = 14; hID[6, 3] = 12; hID[6, 4] = 4; hID[6, 5] = 14; hID[6, 6] = 2; hID[6, 7] = 4;
            hID[7, 0] = 13;  hID[7, 1] = 2;  hID[7, 2] = 14; hID[7, 3] = 13; hID[7, 4] = 3; hID[7, 5] = 14; hID[7, 6] = 2; hID[7, 7] = 3;

            hID[8, 0] = 9;   hID[8, 1] = 3;  hID[8, 2] = 13;  hID[8, 3] = 9;   hID[8, 4] = 7;  hID[8, 5] = 13;  hID[8, 6] = 3; hID[8, 7] = 7;
            hID[9, 0] = 10;  hID[9, 1] = 3;  hID[9, 2] = 13;  hID[9, 3] = 10;  hID[9, 4] = 6;  hID[9, 5] = 13;  hID[9, 6] = 3; hID[9, 7] = 6;
            hID[10, 0] = 11; hID[10, 1] = 3; hID[10, 2] = 13; hID[10, 3] = 11; hID[10, 4] = 5; hID[10, 5] = 13; hID[10, 6] = 3; hID[10, 7] = 5;
            hID[11, 0] = 12; hID[11, 1] = 3; hID[11, 2] = 13; hID[11, 3] = 12; hID[11, 4] = 4; hID[11, 5] = 13; hID[11, 6] = 3; hID[11, 7] = 4;
            hID[12, 0] = 13; hID[12, 1] = 3; hID[12, 2] = 13; hID[12, 3] = 13; hID[12, 4] = 3; hID[12, 5] = 13; hID[12, 6] = 3; hID[12, 7] = 3;
            hID[13, 0] = 14; hID[13, 1] = 3; hID[13, 2] = 13; hID[13, 3] = 14; hID[13, 4] = 2; hID[13, 5] = 13; hID[13, 6] = 3; hID[13, 7] = 2;

            hID[14, 0] = 9;  hID[14, 1] = 4; hID[14, 2] = 12; hID[14, 3] = 9;  hID[14, 4] = 7; hID[14, 5] = 12; hID[14, 6] = 4; hID[14, 7] = 7;
            hID[15, 0] = 10; hID[15, 1] = 4; hID[15, 2] = 12; hID[15, 3] = 10; hID[15, 4] = 6; hID[15, 5] = 12; hID[15, 6] = 4; hID[15, 7] = 6;
            hID[16, 0] = 11; hID[16, 1] = 4; hID[16, 2] = 12; hID[16, 3] = 11; hID[16, 4] = 5; hID[16, 5] = 12; hID[16, 6] = 4; hID[16, 7] = 5;
            hID[17, 0] = 12; hID[17, 1] = 4; hID[17, 2] = 12; hID[17, 3] = 12; hID[17, 4] = 4; hID[17, 5] = 12; hID[17, 6] = 4; hID[17, 7] = 4;
            hID[18, 0] = 13; hID[18, 1] = 4; hID[18, 2] = 12; hID[18, 3] = 13; hID[18, 4] = 3; hID[18, 5] = 12; hID[18, 6] = 4; hID[18, 7] = 3;
            hID[19, 0] = 14; hID[19, 1] = 4; hID[19, 2] = 12; hID[19, 3] = 14; hID[19, 4] = 2; hID[19, 5] = 12; hID[19, 6] = 4; hID[19, 7] = 2;

            hID[20, 0] = 9;  hID[20, 1] = 5; hID[20, 2] = 11; hID[20, 3] = 9;  hID[20, 4] = 7; hID[20, 5] = 11; hID[20, 6] = 5; hID[20, 7] = 7;
            hID[21, 0] = 10; hID[21, 1] = 5; hID[21, 2] = 11; hID[21, 3] = 10; hID[21, 4] = 6; hID[21, 5] = 11; hID[21, 6] = 5; hID[21, 7] = 6;
            hID[22, 0] = 11; hID[22, 1] = 5; hID[22, 2] = 11; hID[22, 3] = 11; hID[22, 4] = 5; hID[22, 5] = 11; hID[22, 6] = 5; hID[22, 7] = 5;
            hID[23, 0] = 12; hID[23, 1] = 5; hID[23, 2] = 11; hID[23, 3] = 12; hID[23, 4] = 4; hID[23, 5] = 11; hID[23, 6] = 5; hID[23, 7] = 4;
            hID[24, 0] = 13; hID[24, 1] = 5; hID[24, 2] = 11; hID[24, 3] = 13; hID[24, 4] = 3; hID[24, 5] = 11; hID[24, 6] = 5; hID[24, 7] = 3;
            hID[25, 0] = 14; hID[25, 1] = 5; hID[25, 2] = 11; hID[25, 3] = 14; hID[25, 4] = 2; hID[25, 5] = 11; hID[25, 6] = 5; hID[25, 7] = 2;
            hID[26, 0] = 15; hID[26, 1] = 5; hID[26, 2] = 11; hID[26, 3] = 15; hID[26, 4] = 1; hID[26, 5] = 11; hID[26, 6] = 5; hID[26, 7] = 1;
      
            hID[27, 0] = 9;  hID[27, 1] = 6; hID[27, 2] = 10; hID[27, 3] = 9;  hID[27, 4] = 7; hID[27, 5] = 10; hID[27, 6] = 6; hID[27, 7] = 7;
            hID[28, 0] = 10; hID[28, 1] = 6; hID[28, 2] = 10; hID[28, 3] = 10; hID[28, 4] = 6; hID[28, 5] = 10; hID[28, 6] = 6; hID[28, 7] = 6;
            hID[29, 0] = 11; hID[29, 1] = 6; hID[29, 2] = 10; hID[29, 3] = 11; hID[29, 4] = 5; hID[29, 5] = 10; hID[29, 6] = 6; hID[29, 7] = 5;
            hID[30, 0] = 12; hID[30, 1] = 6; hID[30, 2] = 10; hID[30, 3] = 12; hID[30, 4] = 4; hID[30, 5] = 10; hID[30, 6] = 6; hID[30, 7] = 4;
            hID[31, 0] = 13; hID[31, 1] = 6; hID[31, 2] = 10; hID[31, 3] = 13; hID[31, 4] = 3; hID[31, 5] = 10; hID[31, 6] = 6; hID[31, 7] = 3;
            hID[32, 0] = 14; hID[32, 1] = 6; hID[32, 2] = 10; hID[32, 3] = 14; hID[32, 4] = 2; hID[32, 5] = 10; hID[32, 6] = 6; hID[32, 7] = 2;
            hID[33, 0] = 15; hID[33, 1] = 6; hID[33, 2] = 10; hID[33, 3] = 15; hID[33, 4] = 1; hID[33, 5] = 10; hID[33, 6] = 6; hID[33, 7] = 1;
 
            hID[34, 0] = 9;  hID[34, 1] = 7; hID[34, 2] = 9;  hID[34, 3] = 9;  hID[34, 4] = 7; hID[34, 5] =  9; hID[34, 6] = 7; hID[34, 7] = 7;
            hID[35, 0] = 10; hID[35, 1] = 7; hID[35, 2] = 9;  hID[35, 3] = 10; hID[35, 4] = 6; hID[35, 5] =  9; hID[35, 6] = 7; hID[35, 7] = 6;
            hID[36, 0] = 11; hID[36, 1] = 7; hID[36, 2] = 9;  hID[36, 3] = 11; hID[36, 4] = 5; hID[36, 5] =  9; hID[36, 6] = 7; hID[36, 7] = 5;
            hID[37, 0] = 12; hID[37, 1] = 7; hID[37, 2] = 9;  hID[37, 3] = 12; hID[37, 4] = 4; hID[37, 5] =  9; hID[37, 6] = 7; hID[37, 7] = 4;
            hID[38, 0] = 13; hID[38, 1] = 7; hID[38, 2] = 9;  hID[38, 3] = 13; hID[38, 4] = 3; hID[38, 5] =  9; hID[38, 6] = 7; hID[38, 7] = 3;
            hID[39, 0] = 14; hID[39, 1] = 7; hID[39, 2] = 9;  hID[39, 3] = 14; hID[39, 4] = 2; hID[39, 5] =  9; hID[39, 6] = 7; hID[39, 7] = 2;
            hID[40, 0] = 15; hID[40, 1] = 7; hID[40, 2] = 9;  hID[40, 3] = 15; hID[40, 4] = 1; hID[40, 5] =  9; hID[40, 6] = 7; hID[40, 7] = 1;

            hID[41, 0] = 9;  hID[41, 1] = 8; hID[41, 2] = 8;  hID[41, 3] = 9;  hID[41, 4] = 7; hID[41, 5] =  8; hID[41, 6] = 8; hID[41, 7] = 7;
            hID[42, 0] = 10; hID[42, 1] = 8; hID[42, 2] = 8;  hID[42, 3] = 10; hID[42, 4] = 6; hID[42, 5] =  8; hID[42, 6] = 8; hID[42, 7] = 6;
            hID[43, 0] = 11; hID[43, 1] = 8; hID[43, 2] = 8;  hID[43, 3] = 11; hID[43, 4] = 5; hID[43, 5] =  8; hID[43, 6] = 8; hID[43, 7] = 5;
            hID[44, 0] = 12; hID[44, 1] = 8; hID[44, 2] = 8;  hID[44, 3] = 12; hID[44, 4] = 4; hID[44, 5] =  8; hID[44, 6] = 8; hID[44, 7] = 4;
            hID[45, 0] = 13; hID[45, 1] = 8; hID[45, 2] = 8;  hID[45, 3] = 13; hID[45, 4] = 3; hID[45, 5] =  8; hID[45, 6] = 8; hID[45, 7] = 3;
            hID[46, 0] = 14; hID[46, 1] = 8; hID[46, 2] = 8;  hID[46, 3] = 14; hID[46, 4] = 2; hID[46, 5] =  8; hID[46, 6] = 8; hID[46, 7] = 2;
            hID[47, 0] = 15; hID[47, 1] = 8; hID[47, 2] = 8;  hID[47, 3] = 15; hID[47, 4] = 1; hID[47, 5] =  8; hID[47, 6] = 8; hID[47, 7] = 1;
        }

        private string GetBEGruppe(int sr, int sc)
        {
            int cx;
            int cy;
            string gruppe = "(H,8)";

            cx = sc + 1; cy = 15 - sr;

            for (int j = 0; j < 48; j++)
            {
                if ((cx == hID[j, 0] && cy == hID[j, 1]) || (cx == hID[j, 2] && cy == hID[j, 3]) || (cx == hID[j, 4] && cy == hID[j, 5]) || (cx == hID[j, 6] && cy == hID[j, 7]))
                {
                    gruppe = "(" + ConvertRowtoY(15-hID[j, 1]) + "," + ConvertColtoX(hID[j, 0]-1) + ")";
                }
            }

            return gruppe;
        }
        private string GetBEGruppePond(int sr, int sc)
        {
            string gruppe = "-";

            gruppe = "Be(" + sr.ToString() +","+sc.ToString()+ ")";

            return gruppe;
        }

        private string GetBEname(string sx, string sy)
        {
            string ret = "-";
            string x1, y1;
            string x2, y2;

            x1 = sx.Trim();
            y1 = sy.Trim();

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {

                x2 = x.xcoord.Trim();
                y2 = x.ycoord.Trim();

                if (x2.Equals(x1)==true && y2.Equals(y1)==true) { ret = x.BEname; break; }

                // ret = x.BEname;
            }

            return ret;
        }
        private double GetBEburnup(string sname)
        {
            double ret = 0.0;
            string bu;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {
                if (x.BEname.Equals(sname) == true) 
                {
                    bu = x.burnup.Trim();
                    if (bu.Length < 2)
                    { ret = -1.0; break; }
                    else
                    { ret = Convert.ToDouble((x.burnup).Replace(".", ",")); break; } 
                }
            }

            return ret;
        }

        // Get numerical coordinates of source fuel assemblies which are homologous in pond
        private void CalcSourceHomolPond(int sr, int sc)
        {

                    SourceHomol[0, 0] = sr; SourceHomol[0, 1] = 0;
                    SourceHomol[1, 0] = sr; SourceHomol[1, 1] = 1;
                    SourceHomol[2, 0] = sr; SourceHomol[2, 1] = 2;
                    SourceHomol[3, 0] = sr; SourceHomol[3, 1] = 3;

                    if (sc == 0) SourceQuad = 1;
                    if (sc == 1) SourceQuad = 2;
                    if (sc == 2) SourceQuad = 3;
                    if (sc == 3) SourceQuad = 4;
        }
        // Get numerical coordinates of target fuel assemblies which are homologous in pond
        private void CalcTargetHomolPond(int sr, int sc)
        {

            TargetHomol[0, 0] = sr; TargetHomol[0, 1] = 0;
            TargetHomol[1, 0] = sr; TargetHomol[1, 1] = 1;
            TargetHomol[2, 0] = sr; TargetHomol[2, 1] = 2;
            TargetHomol[3, 0] = sr; TargetHomol[3, 1] = 3;

            if (sc == 0) TargetQuad = 1;
            if (sc == 1) TargetQuad = 2;
            if (sc == 2) TargetQuad = 3;
            if (sc == 3) TargetQuad = 4;
        }

        private int GetPondPositionX(String regs)
        {
            int ret;
            ret = 0;

            int c1, c2;
            string posx,posy;

            c1 = regs.LastIndexOf(",");
            c2 = regs.LastIndexOf(")");

            // ShowError(regs.ToString()+" "+c1.ToString()+" "+c2.ToString()); 

            posx = regs.Substring(3, c1 - 3).Trim();
            posy = regs.Substring(c1 + 1, c2-c1-1).Trim();

            ret = Convert.ToInt16(posx);

            return ret;
        }
        private int GetPondPositionY(String regs)
        {
            int ret;
            ret = 0;

            int c1, c2;
            string posx, posy;

            c1 = regs.LastIndexOf(",");
            c2 = regs.LastIndexOf(")");

            posx = regs.Substring(3, c1 - 3).Trim();
            posy = regs.Substring(c1 + 1, c2 - c1 - 1).Trim();

            ret = Convert.ToInt16(posy);

            return ret;
        }

        private void HyperlinkButton_MouseEnter(object sender, MouseEventArgs e)
        {

        Image MyImaga=new Image();

        MyImaga.Source = new BitmapImage(new Uri("../Resources/mariussedl black02.jpg", UriKind.Relative));
        MyImaga.Height = 8;
        MSbutton.Content = MyImaga;

        }
        private void HyperlinkButton_MouseLeave(object sender, MouseEventArgs e)
        {

            Image MyImaga = new Image();

            MyImaga.Source = new BitmapImage(new Uri("../Resources/mariussedl red02.jpg", UriKind.Relative));
            MyImaga.Height = 8;
            MSbutton.Content = MyImaga;

        }
        private void HyperlinkButton_MouseEnter_1(object sender, MouseEventArgs e)
        {

            Image MyImaga = new Image();

            MyImaga.Source = new BitmapImage(new Uri("../Resources/NS contact black02.jpg", UriKind.Relative));
            MyImaga.Height = 8;
            CTbutton.Content = MyImaga;

        }
        private void HyperlinkButton_MouseLeave_1(object sender, MouseEventArgs e)
        {

            Image MyImaga = new Image();

            MyImaga.Source = new BitmapImage(new Uri("../Resources/NS contact red02.jpg", UriKind.Relative));
            MyImaga.Height = 8;
            CTbutton.Content = MyImaga;
        }

        private void MyNewsGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {

        MyNewsGrid.Columns[0].MaxWidth = 90;
        MyNewsGrid.Columns[1].MaxWidth = 90;
        MyNewsGrid.Columns[2].MaxWidth = 90;
        MyNewsGrid.Columns[3].MaxWidth = 90;
        MyNewsGrid.Columns[4].MaxWidth = 90;
        MyNewsGrid.Columns[5].MaxWidth = 90;
        MyNewsGrid.Columns[6].MaxWidth = 90;
        MyNewsGrid.Columns[7].MaxWidth = 90;
        MyNewsGrid.Columns[8].MaxWidth = 90;
        MyNewsGrid.Columns[9].MaxWidth = 90;

        }
        private void MyNewsGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
         
        if(e.Property.Name == "Headline") {;}

        if(e.Property.Name == "Datum") {;}

        if(e.Property.Name == "Link") { /* e.Column = new DataGridLinkColumn(e.Property) */; }

        }

        // Default Liste der Brennelemente laden
        private void LoadMyNewsGrid()
        {
            XDocument LoaderDoc;
            LoaderDoc = XDocument.Load("core_default_map.xml");
            List<brennelement> MyData = new List<brennelement>();
            // Server.Mappath();
           
            foreach (XElement x in LoaderDoc.Descendants("FuelAssembly"))
            {
                MyData.Add(new brennelement(x.Attribute("BEname").Value, x.Attribute("keff").Value, x.Attribute("burnup").Value, x.Attribute("region").Value, x.Attribute("location").Value, x.Attribute("xcoordinates").Value, x.Attribute("ycoordinates").Value, x.Attribute("rotation").Value, x.Attribute("zustand").Value, x.Attribute("HMmass").Value, x.Attribute("notes").Value));
            }

            // for (int i = 1; i < 200; i++)  MyData.Add(new brennelement("AAA","1.0","1.0","28A","Kern","10","A","okay","500","1.0","1.0"));
            

        MyNewsGrid.ItemsSource = MyData;
        }

        // Update Liste der Brennelemente during manual interaction
        private void UpdateMyNewsGrid(string xfile)
        {
            if (String.Equals(xfile,"default"))   { return; }
            // Uri loc = new Uri(xfile);
            XDocument LoaderDoc;
            //LoaderDoc = XDocument.Load("."+loc.LocalPath);
            LoaderDoc=XDocument.Parse(xfile);

            List<brennelement> MyData = new List<brennelement>();
            brennelement y;

            foreach (XElement x in LoaderDoc.Descendants("FuelAssembly"))
            {
                y = new brennelement(x.Attribute("BEname").Value, x.Attribute("keff").Value, x.Attribute("burnup").Value, x.Attribute("region").Value, x.Attribute("location").Value, x.Attribute("xcoordinates").Value, x.Attribute("ycoordinates").Value, x.Attribute("rotation").Value, x.Attribute("zustand").Value, x.Attribute("HMmass").Value, x.Attribute("notes").Value);
                y.gruppe = x.Attribute("gruppe").Value;
                MyData.Add(y);
            }

            MyNewsGrid.ItemsSource = MyData;
            // MyNewsGrid.Reload(true);
            // MyNewsGrid.UpdateLayout();
            MyNewsGrid.Refresh();
        }
        // Formatierung Brennelement Daten
        private void MyNewsGrid_LoadedCellPresenter(object sender, DataGridCellEventArgs e)
        {

        if (e.Cell.Column.Name == "BEname") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Bold; e.Cell.Column.Width = new DataGridLength(60); }
        if(e.Cell.Column.Name == "keff") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal;}
        if(e.Cell.Column.Name == "burnup") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal;}
        if (e.Cell.Column.Name == "BEmass") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal; }
        if(e.Cell.Column.Name == "region") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal;}
        if(e.Cell.Column.Name == "location") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal;}

        if(e.Cell.Column.Name == "xcoord") {e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal;}
        if (e.Cell.Column.Name == "ycoord") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal; }
        if (e.Cell.Column.Name == "rotation") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal; }
        if (e.Cell.Column.Name == "zustand") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal; }
        if (e.Cell.Column.Name == "notiz") { e.Cell.Presenter.FontWeight = System.Windows.FontWeights.Normal; }
        }
        // Upload xml file mechanism
        private void btnFileUpload_Click(object sender, RoutedEventArgs e)
        {
            
            if (!IsValidFilePickerSelection())
            {
                return;
            }
            

            // Create uploader..
            _uploader = CreateUploader(FilesPerRequest);
            if (_uploader == null)
            {
                ShowError("Cannot create the uploader");
                return;
            }
            
            // disable the UI
            filebox1.IsEnabled = false;
            btnFileUpload.IsEnabled = false;
            btnAbort.IsEnabled = false;
            btnUpdate.IsEnabled = false;

            // set custom parameters
            _uploader.MaximumUploadSize = (10*1024 * 1024);
            _uploader.AddFiles(filebox1.SelectedFiles);
            _uploader.Parameters["parameter"] = "xml FuelCore Data";

            // handle events
            _uploader.UploadCompleted += new EventHandler<C1.Silverlight.Uploader.UploadCompletedEventArgs>(uploader_UploadCompleted);
            _uploader.UploadProgressChanged += new EventHandler<C1.Silverlight.Uploader.UploadProgressChangedEventArgs>(uploader_UploadProgressChanged);
            try
            {
                _uploader.BeginUploadFiles();
            }
            catch (Exception exc)
            {
                ShowError(exc.Message);
                ResetButtons();
            }
        }

        private bool IsValidFilePickerSelection()
        {
            // check that there are files in the selection
            if (!filebox1.HasSelectedFiles)
            {
                ShowError("Please, select a file prior to upload");
                return false;
            }

            return true;
        }
        private void ResetButtons()
        {
            filebox1.IsEnabled = true;
            btnFileUpload.IsEnabled = true;
            btnAbort.IsEnabled = true;
            btnUpdate.IsEnabled = true;
        }

        private void ShowError(string msg)
        {
            C1MessageBox.Show(msg, "File Uploader Error", C1MessageBoxButton.OK, C1MessageBoxIcon.Error);
        }
        private void ShowErrorIsolatedStorage(string msg)
        {
            C1MessageBox.Show(msg, "Silverlight Isolated Storage Error", C1MessageBoxButton.OK, C1MessageBoxIcon.Error);
        }
        private void ShowStorageComplete(string msg)
        {
            C1MessageBox.Show(msg, "Save completed", C1MessageBoxButton.OK, C1MessageBoxIcon.Information);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.CreateDirectory("corexdir");
                    IsolatedStorageFileStream dirfile = store.CreateFile(System.IO.Path.Combine("corexdir", "coredata.xml"));
                    dirfile.Close();

                    string filepath = System.IO.Path.Combine("corexdir", "coredata.xml");

                    try
                    {
                        using (StreamWriter sw = new StreamWriter(store.OpenFile(filepath, FileMode.Open, FileAccess.Write)))
                        {
                            sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                            sw.WriteLine("<NuclearNews>");
                            sw.WriteLine("<FuelAssemblies>");

                            foreach (brennelement x in MyNewsGrid.ItemsSource)
                            {
                              sw.WriteLine("<FuelAssembly BEname=\""+x.BEname+"\" keff=\""+x.keff+"\" burnup=\""+x.burnup+"\" HMmass=\""+x.HMmass+"\" region=\""+x.region+"\" location=\""+x.location+"\" xcoordinates=\""+x.xcoord+"\" ycoordinates=\""+x.ycoord+"\" rotation=\""+x.rotation+"\" zustand=\""+x.zustand+"\" notes=\""+x.notiz+"\" gruppe=\""+x.gruppe+"\">");
                              sw.WriteLine("</FuelAssembly>");
                            }

                            /*
                            foreach (Piece ps in _tablePieces)
                            {
                                if(ps.Control.Name.Contains("dummy"))
                                {
                                    sw.WriteLine("<FuelAssembly BEname=\"" + ps.Control.Name + "\"" + x.region + "\" location=\"" + x.location + "\" xcoordinates=\"" + x.xcoord + "\" ycoordinates=\"" + x.ycoord + "\">");
                                    sw.WriteLine("</FuelAssembly>");
                                }
                            }
                            */
                            sw.WriteLine("</FuelAssemblies>");
                            sw.WriteLine("</NuclearNews>");
                            ShowStorageComplete("Data has been saved in Silverlight Isolated Storage as corexdir/coredata.xml. \n\n The isolated storage is found on XP and Server2003 at <SYSTEMDRIVE>\\Documents and Settings\\<user>\\Local Setting\\Application Data\\Microsoft\\Silverlight\\is\\... \n\n and on VISTA, Server2008 at <SYSTEMDrive>\\Users\\AppData\\LocalLow\\Microsoft\\Silverlight\\is\\... ");
                        }
                    }
                    catch (IsolatedStorageException ex)
                    {
                        ShowErrorIsolatedStorage(ex.ToString());
                    }
                }
            }
            catch (IsolatedStorageException ex)
            {
                ShowErrorIsolatedStorage(ex.ToString());
            }

        }

        // Uploader file event handler
        #region Handle Uploader Events
        void uploader_UploadProgressChanged(object sender, C1.Silverlight.Uploader.UploadProgressChangedEventArgs e)
        {
            progressGrid.ColumnDefinitions[0].Width = new GridLength(e.ProgressPercentage, GridUnitType.Star);
            progressGrid.ColumnDefinitions[1].Width = new GridLength(100 - e.ProgressPercentage, GridUnitType.Star);
            progressGrid.Visibility = Visibility.Visible;
            lblPercentage.Text = e.ProgressPercentage + "%";
        }
        void uploader_UploadCompleted(object sender, UploadCompletedEventArgs e)
        {
            // clean UI
            ResetButtons();
            progressGrid.Visibility = Visibility.Collapsed;

            if (e.Success)
            {
                xmlfile = ProcessResponse(e.Response)[0];
                UpdateMyNewsGrid(xmlfile);
                CreateUpdateCore();
                CreateUpdatePond();
                // foreach (string link in ProcessResponse(e.Response)) {// linksPanel.Children.Add(CreateLink(link));}
                // ShowError(link);
            }
            else
            {
                // ShowError(e.Error.Message);
                ShowError("File upload could not be completed!");
            }
        }
        private string[] ProcessResponse(object response)
        {
            List<string> result = new List<string>();
            if (response is string[])
            {
                foreach (string responseString in response as string[])
                {
                    foreach (var link in responseString.Split(new char[] { '|' }))
                    {
                        if (!result.Contains(link))
                        {
                            result.Add(link);
                        }
                    }
                }
            }
            return result.ToArray();
        }
        #endregion

        // create file event handler
        #region Create Uploader
        private C1Uploader CreateUploader(FilesPerRequest filesPerRequest)
        {
                    C1UploaderPost mpUploader = new C1UploaderPost(filesPerRequest);
                    mpUploader.Settings.Address = GetHandlerAddress(filesPerRequest);
                    return mpUploader;
        }
        private static Uri GetHandlerAddress(FilesPerRequest filesPerRequest)
        {
            return C1.Silverlight.Extensions.GetAbsoluteUri(filesPerRequest == FilesPerRequest.OneFilePerRequest  ? "MergeHandler.ashx" : "Handler.ashx");
        }
        #endregion

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            MyNewsGrid.Reload(true);
            MyNewsGrid.UpdateLayout();
            MyNewsGrid.Refresh();
        }

        private void CreateNewCore()
        {
            ClearTable();

            // put pieces on the table 
            Style newStyle;
            int posx, posy;
            posx = -1; posy = -1;
            double burny;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {
                posx = -1; posy = -1;
            /*
            Canvas ca1 = new Canvas(); ca1.Width = 50; ca1.Height = 50;
            Rectangle re1 = new Rectangle(); re1.Width = 42; re1.Height = 42; re1.Margin = new Thickness(1); re1.StrokeThickness = 1; re1.RenderTransformOrigin = new Point(0.5, 0.5);
            Canvas.SetTop(ca1, 1); Canvas.SetLeft(ca1, 1);
            ca1.Children.Add(re1);

            ControlTemplate ct = new ControlTemplate(); ct.TargetType = typeof(PieceControl);
            ct.VisualTree=ca1;
            newStyle.Setters.Add(new Setter(Template, 0));
            */
            // --------------------------------------------------------
            //var p1 = new Player() { PieceStyle=(Style)Resources["Player1"] };

            if (x.xcoord.Contains("1") == true) { posx = 0; }
            if (x.xcoord.Contains("2") == true) { posx = 1; }
            if (x.xcoord.Contains("3") == true) { posx = 2; }
            if (x.xcoord.Contains("4") == true) { posx = 3; }
            if (x.xcoord.Contains("5") == true) { posx = 4; }
            if (x.xcoord.Contains("6") == true) { posx = 5; }
            if (x.xcoord.Contains("7") == true) { posx = 6; }
            if (x.xcoord.Contains("8") == true) { posx = 7; }
            if (x.xcoord.Contains("9") == true) { posx = 8; }
            if (x.xcoord.Contains("10") == true) { posx = 9; }
            if (x.xcoord.Contains("11") == true) { posx = 10; }
            if (x.xcoord.Contains("12") == true) { posx = 11; }
            if (x.xcoord.Contains("13") == true) { posx = 12; }
            if (x.xcoord.Contains("14") == true) { posx = 13; }
            if (x.xcoord.Contains("15") == true) { posx = 14; }

            if (x.ycoord.Contains("A") == true) { posy = 14; }
            if (x.ycoord.Contains("B") == true) { posy = 13; }
            if (x.ycoord.Contains("C") == true) { posy = 12; }
            if (x.ycoord.Contains("D") == true) { posy = 11; }
            if (x.ycoord.Contains("E") == true) { posy = 10; }
            if (x.ycoord.Contains("F") == true) { posy = 9; }
            if (x.ycoord.Contains("G") == true) { posy = 8; }
            if (x.ycoord.Contains("H") == true) { posy = 7; }
            if (x.ycoord.Contains("J") == true) { posy = 6; }
            if (x.ycoord.Contains("K") == true) { posy = 5; }
            if (x.ycoord.Contains("L") == true) { posy = 4; }
            if (x.ycoord.Contains("M") == true) { posy = 3; }
            if (x.ycoord.Contains("N") == true) { posy = 2; }
            if (x.ycoord.Contains("O") == true) { posy = 1; }
            if (x.ycoord.Contains("P") == true) { posy = 0; }

            if (posx >= 0 && posy >= 0 && x.location.Contains("Kern")==true)
            {

                newStyle = new Style();
                newStyle.BasedOn = (Style)Resources["Player1"];
                newStyle.TargetType = typeof(PieceControl);
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, x.BEname));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, x.region));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, x.keff));

                // newStyle.Setters.Add(new Setter(PieceControl.BEcolorStartProperty, Colors.White));
                // newStyle.Setters.Add(new Setter(PieceControl.BEcolorEndProperty, Colors.LightGray));

                // 
                burny = Convert.ToDouble((x.burnup).Replace(".",","));
                
                GradientStopCollection gsc = new GradientStopCollection(); GradientStop gs1 = new GradientStop(); GradientStop gs2 = new GradientStop();

                if (burny < 10)
                { // White FromArgb(255,211,211,211)
                    gs1.Color = Colors.White; gs1.Offset = 1; gs2.Color = Colors.White;
                }

                if (burny < 20 && burny >= 10)
                {  // LightGreen FromArgb(255, 152, 251, 152)
                    // gs1.Color = Color.FromArgb(255, 152, 251, 152); gs1.Offset = 1; gs2.Color = Color.FromArgb(255, 152, 251, 152);
                    gs1.Color = Colors.Yellow; gs1.Offset = 1; gs2.Color = Colors.Yellow;
                }

                if (burny < 30 && burny >= 20)
                {  // FromArgb(255, 255, 99, 71)
                    // gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                    gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                }

                if (burny < 40 && burny >= 30)
                { // FromArgb(255, 135, 206, 235)
                   //  gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                    gs1.Color = Colors.Red; gs1.Offset = 1; gs2.Color = Colors.Red;
                }

                if (burny < 50 && burny >= 40)
                { // FromArgb(255, 210, 105, 30)
                   //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                    gs1.Color = Colors.Green; gs1.Offset = 1; gs2.Color = Colors.Green;
                }

                if (burny < 70 && burny >= 50)
                { // FromArgb(255, 210, 105, 30)
                    //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                    gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                }


                gsc.Add(gs1); gsc.Add(gs2);

                newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc,0)));
                var p1 = new Player() { PieceStyle = newStyle };
                PutPiecesName(p1, posx, posy,x.BEname);
            }

            }

        }
        private void CreateNewPond()
        {
            ClearTablePond();
           
            // put pieces on the table 
            Style newStyle;
            int posx, posy;
            posx = 0; posy = -1;
            double burny;
            int lns;
            lns = 0;
            GradientStopCollection gsc; GradientStop gs1; GradientStop gs2;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {

                if (x.location.Contains("Becken") == true && x.zustand.Contains("ok")==true)  
                {
                    lns = lns + 1;
                    posy = posy + 1;
                    if (posy <= 3) 
                    { 
                        // do nothing
                        ; 
                    } else { posy = 0; posx = posx + 1; }

                  
                    newStyle = new Style();
                    newStyle.BasedOn = (Style)Resources["Player1"];
                    newStyle.TargetType = typeof(PieceControl);
                    newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, x.BEname));
                    newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, x.region));
                    newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, x.keff));

                    burny = Convert.ToDouble((x.burnup).Replace(".", ","));

                    gsc = new GradientStopCollection(); gs1 = new GradientStop(); gs2 = new GradientStop();

                    if (burny < 10)
                    { // White FromArgb(255,211,211,211)
                        gs1.Color = Colors.White; gs1.Offset = 1; gs2.Color = Colors.White;
                    }

                    if (burny < 20 && burny >= 10)
                    {  // LightGreen FromArgb(255, 152, 251, 152)
                        // gs1.Color = Color.FromArgb(255, 152, 251, 152); gs1.Offset = 1; gs2.Color = Color.FromArgb(255, 152, 251, 152);
                        gs1.Color = Colors.Yellow; gs1.Offset = 1; gs2.Color = Colors.Yellow;
                    }

                    if (burny < 30 && burny >= 20)
                    {  // FromArgb(255, 255, 99, 71)
                        // gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                        gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                    }

                    if (burny < 40 && burny >= 30)
                    { // FromArgb(255, 135, 206, 235)
                        //  gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                        gs1.Color = Colors.Red; gs1.Offset = 1; gs2.Color = Colors.Red;
                    }

                    if (burny < 50 && burny >= 40)
                    { // FromArgb(255, 210, 105, 30)
                        //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                        gs1.Color = Colors.Green; gs1.Offset = 1; gs2.Color = Colors.Green;
                    }

                    if (burny < 70 && burny >= 50)
                    { // FromArgb(255, 210, 105, 30)
                        //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                        gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                    }

                    gsc.Add(gs1); gsc.Add(gs2);

                    newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));
                    var p1 = new Player() { PieceStyle = newStyle };
                    PutPiecesPondName(p1, posx, posy,x.BEname);
                }
            }

            // fill empty space which is equal to dummy fuel positions
           
            List<brennelement> MyData =(List<brennelement>)MyNewsGrid.ItemsSource;

            for (int i = 0; i < 60; i++)
            {

                    lns = lns + 1;
                    posy = posy + 1;
                    if (posy <= 3) 
                    { 
                        // do nothing
                        ; 
                    } else { posy = 0; posx = posx + 1; }

                newStyle = new Style();
                newStyle.BasedOn = (Style)Resources["Player1"];
                newStyle.TargetType = typeof(PieceControl);
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, lns.ToString()));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, ""));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, ""));
                gsc = new GradientStopCollection(); gs1 = new GradientStop(); gs2 = new GradientStop();
                gs1.Color = Colors.LightGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                gsc.Add(gs1); gsc.Add(gs2);
                newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));

                //sedl 080912 leads to CRASH!! MyData.Add(new brennelement("("+i.ToString()+")", "","", "dummy", "Becken", "-","-", "", "", "", ""));
                PutPiecesPondName(new Player() { PieceStyle = newStyle }, posx, posy, "(" + i.ToString() + ")");
            }

            //sedl 080912 MyNewsGrid.ItemsSource = MyData;

        }
        private void CreateUpdateCore()
        {
            ClearTable();

            // put pieces on the table 
            Style newStyle;
            int posx, posy;
            posx = -1; posy = -1;
            double burny;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {
                posx = -1; posy = -1;

                if (x.xcoord.Contains("1") == true) { posx = 0; }
                if (x.xcoord.Contains("2") == true) { posx = 1; }
                if (x.xcoord.Contains("3") == true) { posx = 2; }
                if (x.xcoord.Contains("4") == true) { posx = 3; }
                if (x.xcoord.Contains("5") == true) { posx = 4; }
                if (x.xcoord.Contains("6") == true) { posx = 5; }
                if (x.xcoord.Contains("7") == true) { posx = 6; }
                if (x.xcoord.Contains("8") == true) { posx = 7; }
                if (x.xcoord.Contains("9") == true) { posx = 8; }
                if (x.xcoord.Contains("10") == true) { posx = 9; }
                if (x.xcoord.Contains("11") == true) { posx = 10; }
                if (x.xcoord.Contains("12") == true) { posx = 11; }
                if (x.xcoord.Contains("13") == true) { posx = 12; }
                if (x.xcoord.Contains("14") == true) { posx = 13; }
                if (x.xcoord.Contains("15") == true) { posx = 14; }

                if (x.ycoord.Contains("A") == true) { posy = 14; }
                if (x.ycoord.Contains("B") == true) { posy = 13; }
                if (x.ycoord.Contains("C") == true) { posy = 12; }
                if (x.ycoord.Contains("D") == true) { posy = 11; }
                if (x.ycoord.Contains("E") == true) { posy = 10; }
                if (x.ycoord.Contains("F") == true) { posy = 9; }
                if (x.ycoord.Contains("G") == true) { posy = 8; }
                if (x.ycoord.Contains("H") == true) { posy = 7; }
                if (x.ycoord.Contains("J") == true) { posy = 6; }
                if (x.ycoord.Contains("K") == true) { posy = 5; }
                if (x.ycoord.Contains("L") == true) { posy = 4; }
                if (x.ycoord.Contains("M") == true) { posy = 3; }
                if (x.ycoord.Contains("N") == true) { posy = 2; }
                if (x.ycoord.Contains("O") == true) { posy = 1; }
                if (x.ycoord.Contains("P") == true) { posy = 0; }

                if (posx >= 0 && posy >= 0 && x.location.Contains("Kern") == true)
                {

                    newStyle = new Style();
                    newStyle.BasedOn = (Style)Resources["Player1"];
                    newStyle.TargetType = typeof(PieceControl);
                    newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, x.BEname));

                    GradientStopCollection gsc = new GradientStopCollection(); GradientStop gs1 = new GradientStop(); GradientStop gs2 = new GradientStop();

                    if (x.region.Contains("dummy") == false)
                    {
                        newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, x.region));
                        newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, x.keff));

                        burny = Convert.ToDouble((x.burnup).Replace(".", ","));

                        if (burny < 10)
                        { // White FromArgb(255,211,211,211)
                            gs1.Color = Colors.White; gs1.Offset = 1; gs2.Color = Colors.White;
                        }

                        if (burny < 20 && burny >= 10)
                        {  // LightGreen FromArgb(255, 152, 251, 152)
                            // gs1.Color = Color.FromArgb(255, 152, 251, 152); gs1.Offset = 1; gs2.Color = Color.FromArgb(255, 152, 251, 152);
                            gs1.Color = Colors.Yellow; gs1.Offset = 1; gs2.Color = Colors.Yellow;
                        }

                        if (burny < 30 && burny >= 20)
                        {  // FromArgb(255, 255, 99, 71)
                            // gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                            gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                        }

                        if (burny < 40 && burny >= 30)
                        { // FromArgb(255, 135, 206, 235)
                            //  gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                            gs1.Color = Colors.Red; gs1.Offset = 1; gs2.Color = Colors.Red;
                        }

                        if (burny < 50 && burny >= 40)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Green; gs1.Offset = 1; gs2.Color = Colors.Green;
                        }

                        if (burny < 70 && burny >= 50)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                        }

                    }
                    else
                    {
                        gs1.Color = Colors.LightGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                    }

                    gsc.Add(gs1); gsc.Add(gs2);

                    newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));
                    var p1 = new Player() { PieceStyle = newStyle };
                    PutPiecesName(p1, posx, posy, x.BEname);
                }

            }

        }
        private void CreateUpdatePond()
        {
            ClearTablePond();
            // put pieces on the table 
            Style newStyle;
            int posx, posy;
            posx = 0; posy = -1;
            double burny;

            string regs;

            GradientStopCollection gsc; GradientStop gs1; GradientStop gs2;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {

                if (x.location.Contains("Becken") == true && x.region.Contains("dummy") == true)
                {
                    posx = GetPondPositionX(x.gruppe);
                    posy = GetPondPositionY(x.gruppe);

                    newStyle = new Style();
                    newStyle.BasedOn = (Style)Resources["Player1"];
                    newStyle.TargetType = typeof(PieceControl);
                    newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, x.BEname));
                    gsc = new GradientStopCollection(); gs1 = new GradientStop(); gs2 = new GradientStop();
                    gs1.Color = Colors.LightGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                    gsc.Add(gs1); gsc.Add(gs2);

                    newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));
                    var p1 = new Player() { PieceStyle = newStyle };
                    PutPiecesPondName(p1, posx, posy, x.BEname);
                }

                if (x.location.Contains("Becken") == true && x.zustand.Contains("ok") == true && x.region.Contains("dummy")==false)
                {
                    regs = x.region;
                    if (regs.Length > 0 && x.region.Contains("dummy")==false)
                    {
                        posx = GetPondPositionX(x.gruppe);
                        posy = GetPondPositionY(x.gruppe);

                        newStyle = new Style();
                        newStyle.BasedOn = (Style)Resources["Player1"];
                        newStyle.TargetType = typeof(PieceControl);
                        newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, x.BEname));
                        newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, x.region));
                        newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, x.keff));

                        burny = Convert.ToDouble((x.burnup).Replace(".", ","));

                        gsc = new GradientStopCollection(); gs1 = new GradientStop(); gs2 = new GradientStop();

                        if (burny < 10)
                        { // White FromArgb(255,211,211,211)
                            gs1.Color = Colors.White; gs1.Offset = 1; gs2.Color = Colors.White;
                        }

                        if (burny < 20 && burny >= 10)
                        {  // LightGreen FromArgb(255, 152, 251, 152)
                            // gs1.Color = Color.FromArgb(255, 152, 251, 152); gs1.Offset = 1; gs2.Color = Color.FromArgb(255, 152, 251, 152);
                            gs1.Color = Colors.Yellow; gs1.Offset = 1; gs2.Color = Colors.Yellow;
                        }

                        if (burny < 30 && burny >= 20)
                        {  // FromArgb(255, 255, 99, 71)
                            // gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                            gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                        }

                        if (burny < 40 && burny >= 30)
                        { // FromArgb(255, 135, 206, 235)
                            //  gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                            gs1.Color = Colors.Red; gs1.Offset = 1; gs2.Color = Colors.Red;
                        }

                        if (burny < 50 && burny >= 40)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Green; gs1.Offset = 1; gs2.Color = Colors.Green;
                        }

                        if (burny < 70 && burny >= 50)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                        }

                        gsc.Add(gs1); gsc.Add(gs2);

                        newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));
                        var p1 = new Player() { PieceStyle = newStyle };
                        PutPiecesPondName(p1, posx, posy, x.BEname);
                    }


                }
            }

        }
        private void CreateUpdateCoreResults()
        {
            // ClearTable();
            // put pieces on the table 
            Style newStyle;
            int posx, posy;
            posx = -1; posy = -1;
            double burny;
            string benam="";


            foreach (brennelementresults x in MyDatasResult)
            {
                posy = ConvertXtoCol(x.xcoord);
                posx = ConvertYtoRow(x.ycoord);

                benam = GetBEname(x.xcoord, x.ycoord);
                burny = GetBEburnup(benam);


                newStyle = new Style();
                newStyle.BasedOn = (Style)Resources["Player1"];
                newStyle.TargetType = typeof(PieceControl);
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEnameProperty, benam));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEregionProperty, x.wxyz));
                newStyle.Setters.Add(new Setter(PieceControl.CaptionBEkeffProperty, x.dnbr));
                
                GradientStopCollection gsc = new GradientStopCollection(); GradientStop gs1 = new GradientStop(); GradientStop gs2 = new GradientStop();

                        if (burny < 10)
                        { // White FromArgb(255,211,211,211)
                            gs1.Color = Colors.White; gs1.Offset = 1; gs2.Color = Colors.White;
                        }

                        if (burny < 20 && burny >= 10)
                        {  // LightGreen FromArgb(255, 152, 251, 152)
                            // gs1.Color = Color.FromArgb(255, 152, 251, 152); gs1.Offset = 1; gs2.Color = Color.FromArgb(255, 152, 251, 152);
                            gs1.Color = Colors.Yellow; gs1.Offset = 1; gs2.Color = Colors.Yellow;
                        }

                        if (burny < 30 && burny >= 20)
                        {  // FromArgb(255, 255, 99, 71)
                            // gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                            gs1.Color = Colors.Orange; gs1.Offset = 1; gs2.Color = Colors.Orange;
                        }

                        if (burny < 40 && burny >= 30)
                        { // FromArgb(255, 135, 206, 235)
                            //  gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                            gs1.Color = Colors.Red; gs1.Offset = 1; gs2.Color = Colors.Red;
                        }

                        if (burny < 50 && burny >= 40)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Green; gs1.Offset = 1; gs2.Color = Colors.Green;
                        }

                        if (burny < 70 && burny >= 50)
                        { // FromArgb(255, 210, 105, 30)
                            //  gs1.Color = Colors.DarkGray; gs1.Offset = 1; gs2.Color = Colors.DarkGray;
                            gs1.Color = Colors.Brown; gs1.Offset = 1; gs2.Color = Colors.Brown;
                        }

                    gsc.Add(gs1); gsc.Add(gs2);

                    newStyle.Setters.Add(new Setter(PieceControl.BEfillProperty, new LinearGradientBrush(gsc, 0)));

                    var p1 = new Player() { PieceStyle = newStyle };
                    PutPiecesSimple(p1, posx, posy);
            }

        }
        private void ClearTable()
        {
            // initiaize table model
            _tablePieces = new Piece[15, 15];

            // initialize Drag & Drop Manager
            _ddManager.ClearSources();
            _ddManager.ClearTargets();

            // create Checkers Table
            CreateTable();
        }
        private void ClearTablePond()
        {
            // initiaize table model
            _tablePiecesPond = new Piece[100, 4];

            // initialize Drag & Drop Manager
            // _ddManager.ClearSources();
            // _ddManager.ClearTargets();

            // create Checkers Table
            CreateTablePond();
        }
        private void PutPieces(Player player,int i, int j)
        {

                    // create pice with team color
                    Piece piece = new Piece(player);
 
                    // locate piece
                    LocatePiece(piece, i, j);

                    // set as drop source
                    _ddManager.RegisterDragSource(piece.Control, DragDropEffect.Move, ModifierKeys.None);

        }
        private void PutPiecesSimple(Player player, int i, int j)
        {

            // create pice with team color
            Piece piece = new Piece(player);

            // locate piece
            LocatePieceSimple(piece, i, j);

            // set as drop source
            // _ddManager.RegisterDragSource(piece.Control, DragDropEffect.None, ModifierKeys.None);

        }
        private void PutPiecesName(Player player, int i, int j, string BEname)
        {

            // create pice with team color
            Piece piece = new Piece(player);
            piece.Control.Name = BEname;
            // locate piece
            LocatePiece(piece, i, j);

            // set as drop source
            _ddManager.RegisterDragSource(piece.Control, DragDropEffect.Move, ModifierKeys.None);

        }
        private void PutPiecesPond(Player player, int i, int j)
        {

            // create pice with team color
            Piece piece = new Piece(player);

            // locate piece
            LocatePiecePond(piece, i, j);

            // set as drop source
            _ddManager.RegisterDragSource(piece.Control, DragDropEffect.Move, ModifierKeys.None);

        }
        private void PutPiecesPondName(Player player, int i, int j,string BEname)
        {

            // create pice with team color
            Piece piece = new Piece(player);
            piece.Control.Name = BEname;
            // locate piece
            LocatePiecePond(piece, i, j);

            // set as drop source
            _ddManager.RegisterDragSource(piece.Control, DragDropEffect.Move, ModifierKeys.None);

        }  
        private void CreateTable()
        {
            _tableBorders = new Border[15,15];
            int pfeil;
            pfeil = 0;

            CoreTable.Children.Clear();
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    // create black or white rectangle 
                    Border back = new Border();
                    // back.Style = (Style)Resources[((j + i) % 2 == 0 ? "BlackBackground" : "WhiteBackground")];
                    back.Style = (Style)Resources["WhiteBackground"]; pfeil = 1;

                    if (i == 0 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 1) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 2) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 3) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 11) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 12) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 13) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 0 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 1 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 1 && j == 1) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 1 && j == 13) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 1 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 2 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 2 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 3 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 3 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 11 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 11 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 12 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 12 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 13 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 13 && j == 1) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 13 && j == 13) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 13 && j == 14) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}

                    if (i == 14 && j == 0) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 1) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 2) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 3) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 11) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 12) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 13) {back.Style = (Style)Resources["BlackBackground"];pfeil=0;}
                    if (i == 14 && j == 14) {back.Style = (Style)Resources["BlackBackground"]; pfeil = 0; }

                    back.IsHitTestVisible = true;
                    back.BorderThickness = new Thickness(0.5);
                    back.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                    // locate in table
                    Grid.SetRow(back, i);
                    Grid.SetColumn(back, j);
                    _tableBorders[i, j] = back;
                    CoreTable.Children.Add(back);

                    // set as drop target
                    if(pfeil==1)    _ddManager.RegisterDropTarget(back, true);
                }
            }
        }
        private void CreateTablePond()
        {
            _tableBordersPond = new Border[100, 4];

            PondTable.Children.Clear();
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    // create black or white rectangle 
                    Border back = new Border();

                    back.Style = (Style)Resources["WhiteBackground"]; 

                    back.IsHitTestVisible = true;
                    back.BorderThickness = new Thickness(0.5);
                    back.BorderBrush = new SolidColorBrush(Colors.DarkGray);
                    back.Width = 50; back.Height = 50;
                    // locate in table
                    Grid.SetRow(back, i);
                    Grid.SetColumn(back, j);
                    _tableBordersPond[i, j] = back;
                    PondTable.Children.Add(back);

                    // set as drop target
                   _ddManager.RegisterDropTarget(back, true);
                }
            }
        }
/*
        private void PutPieces(Player player)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    // create pice with team color
                    Piece piece = new Piece(player);

                    // locate piece
                    int row = player.InitialRow + i;
                    int column = j * 2 + ((i + player.PlayerNumber) % 2);
                    LocatePiece(piece, row, column);

                    // set as drop source
                    _ddManager.RegisterDragSource(piece.Control, DragDropEffect.Move, ModifierKeys.None);
                }
            }
        }
 */
        private void LocatePiece(Piece piece, int row, int column)
        {
            if (piece.Control.Parent != null)
            {
                var border = (Border)piece.Control.Parent;

                // get old row and column
                int oldRow = Grid.GetRow(border);
                int oldColumn = Grid.GetColumn(border);

                // remove from to table
                if ((oldRow >= 0 && oldRow < 15) && (oldColumn >= 0 && oldColumn < 15))
                {
                    _tablePieces[oldRow, oldColumn] = null;
                }
                border.Child = null;
            }

            // add to table
            if ((row >= 0 && row < 15) && (column >= 0 && column < 15))
            {
                _tablePieces[row, column] = piece;
                UpdateBEListe(piece.Control.Name, row, column);
                _tableBorders[row, column].Child = piece.Control;
            }
        }
        private void LocatePieceSimple(Piece piece, int row, int column)
        {
            if (piece.Control.Parent != null)
            {
                var border = (Border)piece.Control.Parent;

                // get old row and column
                int oldRow = Grid.GetRow(border);
                int oldColumn = Grid.GetColumn(border);

                // remove from to table
                if ((oldRow >= 0 && oldRow < 15) && (oldColumn >= 0 && oldColumn < 15))
                {
                   // _tablePieces[oldRow, oldColumn] = null;
                }
                border.Child = null;
            }

            // add to table
            if ((row >= 0 && row < 15) && (column >= 0 && column < 15))
            {
                // _tablePieces[row, column] = piece;
                _tableBorders[row, column].Child = piece.Control;
            }
        }
 
        private void LocatePiecePond(Piece piece, int row, int column)
        {
            if (piece.Control.Parent != null)
            {
                var border = (Border)piece.Control.Parent;

                // get old row and column
                int oldRow = Grid.GetRow(border);
                int oldColumn = Grid.GetColumn(border);

                // remove from to table
                if ((oldRow >= 0 && oldRow < 100) && (oldColumn >= 0 && oldColumn < 4))
                {
                    _tablePiecesPond[oldRow, oldColumn] = null;
                }
                border.Child = null;
            }

            // add to table
            if ((row >= 0 && row < 100) && (column >= 0 && column < 4))
            {
                _tablePiecesPond[row, column] = piece;
                
                if(piece.Control.Name.Length>0) UpdateBEListePond(piece.Control.Name, row, column);

                _tableBordersPond[row, column].Child = piece.Control;
            }
        }
        private void homolog4_Unchecked(object sender, RoutedEventArgs e)
        {
            homolog4you = 0;
        }
        private void homolog4_Checked(object sender, RoutedEventArgs e)
        {
            homolog4you = 1;
        }
        private void dnbr_Checked(object sender, RoutedEventArgs e)
        {
            XDocument LoaderDoc;
            LoaderDoc = XDocument.Load("core_default_results.xml");
            MyDatasResult = new List<brennelementresults>();
            // brennelementresults mys;
            // Server.Mappath();

            foreach (XElement x in LoaderDoc.Descendants("FuelResult"))
            {
               MyDatasResult.Add(new brennelementresults( x.Attribute("xcoordinates").Value, x.Attribute("ycoordinates").Value, x.Attribute("location").Value, x.Attribute("WXYZ").Value, x.Attribute("DNBR").Value));
              //  mys = new brennelementresults(x.Attribute("xcoordinates").Value, x.Attribute("ycoordinates").Value, x.Attribute("location").Value, x.Attribute("WXYZ").Value, x.Attribute("DNBR").Value);
            }

            CreateUpdateCoreResults();
        }
        private void dnbr_Unchecked(object sender, RoutedEventArgs e)
        {
            MyDatasResult.Clear();

            int i, j;
            for (i = 0; i < 15; i++)
            {
                for (j = 0; j < 15; j++)
                {
                    if(_tablePieces[i,j]!=null) LocatePieceSimple(_tablePieces[i, j], i, j);
                }
            }
        }
        private void UpdateBEListe(string BEname, int row, int col)
        {
            string cx;
            string cy;

            int row1;
            int col1;

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {
                if(x.BEname.Contains(BEname))
                {
                    cx = x.xcoord;
                    cy = x.ycoord;
                    col1 = ConvertXtoCol(cx);
                    row1 = ConvertYtoRow(cy);

                    // if ((row1 != row) || (col1 != col))
                    {
                        cx = ConvertColtoX(col);
                        cy = ConvertRowtoY(row);
                        x.xcoord = cx;
                        x.ycoord = cy;
                        x.location = "Kern";
                        x.gruppe = GetBEGruppe(row, col);
                    }
                }

            }
        }
        private void UpdateBEListePond(string BEname, int row, int col)
        {

            foreach (brennelement x in MyNewsGrid.ItemsSource)
            {
                if (x.BEname.Contains(BEname))
                {
                        x.xcoord = "-";
                        x.ycoord = "-";
                        x.location = "Becken";
                        x.gruppe = GetBEGruppePond(row, col);
                }

            }
        }
        private int ConvertXtoCol(string cx)
        {
            int ret;
            ret=-1;

            if (cx.Equals("1") && cx.Length == 1) { ret = 0; return ret; }
            if (cx.Equals("2") && cx.Length == 1) { ret = 1; return ret; }
            if (cx.Equals("3") && cx.Length == 1) { ret = 2; return ret; }
            if (cx.Equals("4") && cx.Length == 1) { ret = 3; return ret; }
            if (cx.Equals("5") && cx.Length == 1) { ret = 4; return ret; }
            if (cx.Equals("6") && cx.Length == 1) { ret = 5; return ret; }
            if (cx.Equals("7") && cx.Length == 1) { ret = 6; return ret; }
            if (cx.Equals("8") && cx.Length == 1) { ret = 7; return ret; }
            if (cx.Equals("9") && cx.Length == 1) { ret = 8; return ret; }
            if (cx.Equals("10")) { ret = 9; return ret; }
            if (cx.Equals("11")) { ret = 10; return ret; }
            if (cx.Equals("12")) { ret = 11; return ret; }
            if (cx.Equals("13")) { ret = 12; return ret; }
            if (cx.Equals("14")) { ret = 13; return ret; }
            if (cx.Equals("15")) { ret = 14; return ret; }

            return ret;
        }
        private int ConvertYtoRow(string cy)
        {
            int ret;
            ret=-1;
            if (cy.Contains("A")) { ret = 14; return ret; }
            if (cy.Contains("B")) { ret = 13; return ret; }
            if (cy.Contains("C")) { ret = 12; return ret; }
            if (cy.Contains("D")) { ret = 11; return ret; }
            if (cy.Contains("E")) { ret = 10; return ret; }
            if (cy.Contains("F")) { ret = 9; return ret; }
            if (cy.Contains("G")) { ret = 8; return ret; }
            if (cy.Contains("H")) { ret = 7; return ret; }
            if (cy.Contains("J")) { ret = 6; return ret; }
            if (cy.Contains("K")) { ret = 5; return ret; }
            if (cy.Contains("L")) { ret = 4; return ret; }
            if (cy.Contains("M")) { ret = 3; return ret; }
            if (cy.Contains("N")) { ret = 2; return ret; }
            if (cy.Contains("O")) { ret = 1; return ret; }
            if (cy.Contains("P")) { ret = 0; return ret; }
            return ret;
        }
        private string ConvertColtoX(int col)
        {
            string ret;
            ret = "0";

            if (col == 0) { ret = "1"; return ret; }
            if (col == 1) { ret = "2"; return ret; }
            if (col == 2) { ret = "3"; return ret; }
            if (col == 3) { ret = "4"; return ret; }
            if (col == 4) { ret = "5"; return ret; }
            if (col == 5) { ret = "6"; return ret; }
            if (col == 6) { ret = "7"; return ret; }
            if (col == 7) { ret = "8"; return ret; }
            if (col == 8) { ret = "9"; return ret; }
            if (col == 9) { ret = "10"; return ret; }
            if (col == 10) { ret = "11"; return ret; }
            if (col == 11) { ret = "12"; return ret; }
            if (col == 12) { ret = "13"; return ret; }
            if (col == 13) { ret = "14"; return ret; }
            if (col == 14) { ret = "15"; return ret; }

            return ret;
        }
        private string ConvertRowtoY(int row)
        {
            string ret;
            ret = "0";

            if (row == 14) { ret = "A"; return ret; }
            if (row == 13) { ret = "B"; return ret; }
            if (row == 12) { ret = "C"; return ret; }
            if (row == 11) { ret = "D"; return ret; }
            if (row == 10) { ret = "E"; return ret; }
            if (row == 9) { ret = "F"; return ret; }
            if (row == 8) { ret = "G"; return ret; }
            if (row == 7) { ret = "H"; return ret; }
            if (row == 6) { ret = "J"; return ret; }
            if (row == 5) { ret = "K"; return ret; }
            if (row == 4) { ret = "L"; return ret; }
            if (row == 3) { ret = "M"; return ret; }
            if (row == 2) { ret = "N"; return ret; }
            if (row == 1) { ret = "O"; return ret; }
            if (row == 0) { ret = "P"; return ret; }

            return ret;
        }     
        private void CoreControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            C1TabControl tx=(C1TabControl)sender;
            if (tx == null) return;

            // if (CoreData.IsSelected==true)
            if(tx.SelectedIndex==1)
            {
                    MyNewsGrid.Reload(true);
                    MyNewsGrid.UpdateLayout();
                    MyNewsGrid.Refresh();

            }

        }
        private void filebox1_TextInput(object sender, TextCompositionEventArgs e)
        {

        }
         
    }

  // Brennelement Eigenschaften - Klasse  
public class brennelement
{
   
    private string BEname00;
    private string keff00;
    private string burnup00;
    private string mass00;
    private string region00;
    private string location00;
    private string gruppe00;
    private string xcoordinates00;
    private string ycoordinates00;
    private string rotation00;
    private string zustand00;
    private string notiz00;

    public brennelement()
    {
       
        BEname = "null";
        HMmass = "null";
        keff = "null";
        burnup = "null";
        region = "null";
        location = "null";
        gruppe = "null";
        xcoord = "null";
        ycoord = "null";
        rotation = "null";
        zustand = "null";
        notiz= "null";
        gruppe = "-";
    }

    public brennelement(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10, string s11)
        {
            BEname = s1;
            HMmass = s10;
            keff = s2;
            burnup = s3;
            region = s4;
            location = s5;
            xcoord = s6;
            ycoord= s7;
            rotation = s8;
            zustand = s9;
            notiz= s11;
            gruppe = "-";
        }

    public string BEname
    {
        get
        {
            return BEname00;
        }

        set
        {
            BEname00 = value;
        }
    }
    public string keff
    {
        get
        {
            return keff00;
        }

        set
        {
            keff00 = value;
        }
    }
    public string burnup
    {
        get
        {
            return burnup00;
        }

        set
        {
            burnup00 =  value;
        }
    }
    public string HMmass
    {
        get
        {
            return mass00;
        }

        set
        {
            mass00 = value;
        }
    }
    public string region
    {
        get
        {
            return region00;
        }

        set
        {
            region00= value;
        }
    }
    public string location
    {
        get
        {
            return location00;
        }

        set
        {
            location00 = value;
        }
    }
    public string gruppe
    {
        get
        {
            return gruppe00;
        }

        set
        {
            gruppe00 = value;
        }
    }
    public string xcoord
    {
        get
        {
            return xcoordinates00;
        }

        set
        {
           xcoordinates00 = value;
        }
    }
    public string ycoord
    {
        get
        {
            return ycoordinates00;
        }

        set
        {
            ycoordinates00 = value;
        }
    }
    public string rotation
    {
        get
        {
            return rotation00;
        }

        set
        {
            rotation00 = value;
        }
    }
    public string zustand
    {
        get
        {
            return zustand00;
        }

        set
        {
            zustand00 = value;
        }
    }
    public string notiz
    {
        get
        {
            return notiz00;
        }

        set
        {
            notiz00 = value;
        }
    }

}

public class brennelementresults
{

    private string BEname00;
    private string keff00;
    private string burnup00;
    private string mass00;
    private string region00;
    private string location00;
    private string gruppe00;
    private string xcoordinates00;
    private string ycoordinates00;
    private string rotation00;
    private string zustand00;
    private string notiz00;

    public brennelementresults()
    {

        BEname = "null";
        keff = "null";
        burnup = "null";
        burnupxy = "null";
        burnupxyz = "null";
        location = "null";
        xcoord = "null";
        ycoord = "null";
        wcm = "null";
        wxy = "null";
        wxyz = "null";
        dnbr = "-";
    }

    public brennelementresults(string s1, string s2, string s3, string s4, string s5)
    {
        BEname = "-";
        keff = "-";
        burnup = "-";
        burnupxy = "-";
        burnupxyz = "-";
        location = s3;
        xcoord = s1;
        ycoord = s2;
        wcm = "-";
        wxy = "-";
        wxyz = s4;
        dnbr = s5;
    }

    public string BEname
    {
        get
        {
            return BEname00;
        }

        set
        {
            BEname00 = value;
        }
    }
    public string keff
    {
        get
        {
            return keff00;
        }

        set
        {
            keff00 = value;
        }
    }
    public string burnup
    {
        get
        {
            return burnup00;
        }

        set
        {
            burnup00 = value;
        }
    }
    public string burnupxy
    {
        get
        {
            return mass00;
        }

        set
        {
            mass00 = value;
        }
    }
    public string burnupxyz
    {
        get
        {
            return region00;
        }

        set
        {
            region00 = value;
        }
    }
    public string location
    {
        get
        {
            return location00;
        }

        set
        {
            location00 = value;
        }
    }

    public string xcoord
    {
        get
        {
            return xcoordinates00;
        }

        set
        {
            xcoordinates00 = value;
        }
    }
    public string ycoord
    {
        get
        {
            return ycoordinates00;
        }

        set
        {
            ycoordinates00 = value;
        }
    }

    public string wcm
    {
        get
        {
            return notiz00;
        }

        set
        {
            notiz00 = value;
        }
    }
    public string wxy
    {
        get
        {
            return gruppe00;
        }

        set
        {
            gruppe00 = value;
        }
    }
    public string wxyz
    {
        get
        {
            return rotation00;
        }

        set
        {
            rotation00 = value;
        }
    }

    public string dnbr
    {
        get
        {
            return zustand00;
        }

        set
        {
            zustand00 = value;
        }
    }


}
    // Piece of core layout
    [TemplatePart(Name = "PART_BEname", Type=typeof(TextBlock))]
    [TemplatePart(Name = "PART_BEregion", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_BEkeff", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_BEcolorStart", Type = typeof(GradientStop))]
    [TemplatePart(Name = "PART_BEcolorEnd", Type = typeof(GradientStop))]
    [TemplatePart(Name = "PART_BEfill", Type = typeof(Rectangle))]

public class PieceControl : Control
{
    public static readonly DependencyProperty CaptionBEnameProperty = DependencyProperty.Register("CaptionBEname", typeof(string), typeof(PieceControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty CaptionBEregionProperty = DependencyProperty.Register("CaptionBEregion", typeof(string), typeof(PieceControl), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty CaptionBEkeffProperty = DependencyProperty.Register("CaptionBEkeff", typeof(string), typeof(PieceControl), new PropertyMetadata(string.Empty));
      
    public static readonly DependencyProperty BEfillProperty = DependencyProperty.Register("BEfill", typeof(LinearGradientBrush), typeof(PieceControl), new PropertyMetadata(new LinearGradientBrush()));


    public string CaptionBEname
    {
        get { return (string)GetValue(CaptionBEnameProperty);}
        set { SetValue(CaptionBEnameProperty, value); }
    }
    public string CaptionBEregion
    {
        get { return (string)GetValue(CaptionBEregionProperty); }
        set { SetValue(CaptionBEregionProperty, value); }
    }
    public string CaptionBEkeff
    {
        get { return (string)GetValue(CaptionBEkeffProperty); }
        set { SetValue(CaptionBEkeffProperty, value); }
    }
    public LinearGradientBrush BEfill
    {
        get { return (LinearGradientBrush)GetValue(BEfillProperty); }
        set { SetValue(BEfillProperty, value); }
    }
    public PieceControl()
    {
        DefaultStyleKey = typeof(PieceControl);
    }
}

public class Piece
{
    public Piece(Player team)
    // public Piece()
    {
        Team = team;
        Control = new PieceControl() { Style = team.PieceStyle };
    }

    public Player Team { get; private set; }
    public FrameworkElement Control { get; private set; }
}

public class Player
{
    public Player()
    {
        ;
    }
    public Style PieceStyle { get; set; }
    
}

/* C1UploaderHelper
public static class C1UploaderHelper
{
    private const string STORAGE_SUBFOLDER_VIRTUAL = "temp/";
    private const string STORAGE_SUBFOLDER = @"temp\";
    public const string ERROR_MESSAGE = "Couldn't upload the file.";


    public static bool ProcessPart(HttpContext context, Stream stream, string serverFileName, int partCount, int partNumber)
    {
        int length = (int)stream.Length;

        // if it's the first part
        if (partNumber == 1)
        {
            FileStream fileStream = File.Open(serverFileName, FileMode.Create);
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            fileStream.Write(buffer, 0, length);

            if (partCount != partNumber)
            {
                // not the last one, save temporarilly
                fileStream.Close();
            }
            else 
            {
                // it's the last one and it's a valid image
                fileStream.Close();
            }

        }
        else
        {
            // put the stream in a buffer
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);

            // if it isn't the last part
            if (partCount != partNumber)
            {
                // read the temporal file saved in the server and append the new part
                FileStream storedFile = File.Open(serverFileName, FileMode.Append);
                storedFile.Write(buffer, 0, buffer.Length);
                storedFile.Close();
            }
            else
            {
                // this is the last part 
                // read the temporal file saved in the server and append the new part
                FileStream storedFile = File.Open(serverFileName, FileMode.Open);
                storedFile.Position = storedFile.Length;
                storedFile.Write(buffer, 0, buffer.Length);
  
                storedFile.Close();


            }
        }

        return true;
    }

    public static string GetUploadedFileUrl(HttpContext context, string handlerName, string fileName)
    {
        return context.Request.Url.AbsoluteUri.Replace(handlerName, STORAGE_SUBFOLDER_VIRTUAL + fileName);
    }

    public static string GetServerPath(HttpServerUtility server, string fileName)
    {
        return server.MapPath(System.IO.Path.Combine(STORAGE_SUBFOLDER, System.IO.Path.GetFileName(fileName)));
    }

    public static void WriteError(HttpContext context, string message)
    {
        context.Response.StatusCode = 500;
        context.Response.Write(message);
    }

    public static void CleanStream(FileStream fileStream, string serverFileName)
    {
        int lenght = (int)fileStream.Length;
        fileStream.Position = 0;
        fileStream.Write(new byte[lenght], 0, lenght);
        fileStream.Close();

        File.Delete(serverFileName);
    }
}
    */


}
