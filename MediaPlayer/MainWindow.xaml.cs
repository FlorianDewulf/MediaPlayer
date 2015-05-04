using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using WinForm = System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using TagLib;

namespace MediaPlayer
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    /// 

    //class qui contient les informations des media pour la bibliothèque
    public class Biblio
    {
        public string Titre { get; set; }
        public string Album { get; set; }
        public string Duree { get; set; }
        public string Genre { get; set; }
        public string Path { get; set; }
    }

    public partial class MainWindow : Window
    {
        List<Biblio> liste_bi;
        Brush currentTrackIndicator;
        Brush TrackColor;
        ListBoxItem currentTrack;
        ListBoxItem PreviousTrack;
        DispatcherTimer timer;
        TagLib.File file;
        bool isDragging;
        bool check_mute;
        bool check_repeat;
        bool check_shuffle;
        int[] shuffle;
        double save_vol;
        string fileName;
        string mPlayer_time;
        string urlplayer_time;
        private object dummyNode = null;
        public string SelectedImagePath { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            init_var();
            init_bibliotheque();
            tracklist.MouseDoubleClick += new MouseButtonEventHandler(tracklist_MouseDoubleClick);
        }

        private void init_var()
        {
            currentTrack = null;
            PreviousTrack = null;
            check_mute = false;
            check_repeat = false;
            check_shuffle = false;
            fileName = string.Empty;
            liste_bi = new List<Biblio>();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(timer_Tick);
            isDragging = false;
            mPlayer_time = "";
            this.MinHeight = 600;
            this.MinWidth = 800;
            currentTrackIndicator = Brushes.MediumSeaGreen;
            TrackColor = tracklist.Foreground;
        }

        private void init_bibliotheque()
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Biblio>));
            try
            {
                using (StreamReader rd = new StreamReader("./bibliotheque.xml"))
                {
                    liste_bi = xs.Deserialize(rd) as List<Biblio>;
                    Bibliotheque.ItemsSource = null;
                    Bibliotheque.ItemsSource = liste_bi;
                 }
            }
            catch (Exception e)
            {
            }
        }

        /*Arbre hierarchique*/

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in Directory.GetLogicalDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = s;
                item.Tag = s;
                item.FontWeight = FontWeights.Normal;
                item.Items.Add(dummyNode);
                item.Expanded += new RoutedEventHandler(folder_Expanded);
                foldersItem.Items.Add(item);
            }
        }

        void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(dummyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Items.Add(subitem);
                    }
                    foreach (string s in Directory.GetFiles(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        string extension = System.IO.Path.GetExtension(subitem.Header.ToString());
                        if (extension.ToLowerInvariant() == ".mp3" || extension.ToLowerInvariant() == ".jpg" ||
                            extension.ToLowerInvariant() == ".avi" || extension.ToLowerInvariant() == ".jpeg" ||
                            extension.ToLowerInvariant() == ".wav" || extension.ToLowerInvariant() == ".wmv" ||
                            extension.ToLowerInvariant() == ".png" || extension.ToLowerInvariant() == ".bmp" ||
                            extension.ToLowerInvariant() == ".mp4")
                        {
                            subitem.Tag = s;
                            subitem.FontWeight = FontWeights.Normal;
                            item.Items.Add(subitem);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        private void foldersItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tree = (TreeView)sender;
            TreeViewItem temp = ((TreeViewItem)tree.SelectedItem);

            if (temp == null)
                return;
            SelectedImagePath = "";
            string temp1 = "";
            string temp2 = "";
            while (true)
            {
                temp1 = temp.Header.ToString();
                if (temp1.Contains(@"\"))
                {
                    temp2 = "";
                }
                SelectedImagePath = temp1 + temp2 + SelectedImagePath;
                if (temp.Parent.GetType().Equals(typeof(TreeView)))
                {
                    break;
                }
                temp = ((TreeViewItem)temp.Parent);
                temp2 = @"\";
            }
        }

        /*Media Element*/

        //charge le media dans le media element pour url ou tracklist

        private void url_player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (url_player.NaturalDuration.HasTimeSpan)
                {
                    TimeSpan ts = url_player.NaturalDuration.TimeSpan;
                    SliderTimeLine.Maximum = ts.TotalSeconds;
                    SliderTimeLine.SmallChange = 1;
                }
                timer.Start();
                url_player.LoadedBehavior = MediaState.Play;
        }

        private void url_player_MediaEnded(object sender, RoutedEventArgs e)
        {
            SliderTimeLine.Value = 0;
            urlplayer_time = "";
            url_player.LoadedBehavior = MediaState.Stop;
        }

        private void mPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (mPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = mPlayer.NaturalDuration.TimeSpan;
                SliderTimeLine.Maximum = ts.TotalSeconds;
                SliderTimeLine.SmallChange = 1;
                time.Text = SliderTimeLine.Value.ToString();
            }
            timer.Start();
        }

        private void mPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (check_repeat == false)
                MoveToNextTrack();
            else if (check_repeat == true)
            {
                mPlayer.Source = new Uri(currentTrack.Tag.ToString());
                SliderTimeLine.Value = 0;
                PlayTrack();
            }
            else if (check_shuffle == true)
                MoveToNextTrack();
            else
            {
                SliderTimeLine.Value = 0;
                mPlayer_time = "";
                mPlayer.LoadedBehavior = MediaState.Stop;
            }
        }
        
        /*Liste Box Track*/

        private void ListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
                e.Effects = DragDropEffects.None;
        }

        //deplacement des fichiers dans la tracklist
        private void listBox1_Drop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string fileName in s)
            {
                string extension = System.IO.Path.GetExtension(fileName);
                if (extension.ToLowerInvariant() == ".mp3" || extension.ToLowerInvariant() == ".jpg" ||
                    extension.ToLowerInvariant() == ".avi" || extension.ToLowerInvariant() == ".jpeg" ||
                    extension.ToLowerInvariant() == ".wav" || extension.ToLowerInvariant() == ".wmv" ||
                    extension.ToLowerInvariant() == ".png" || extension.ToLowerInvariant() == ".bmp" ||
                    extension.ToLowerInvariant() == ".mp4")
                {
                    ListBoxItem lstItem = new ListBoxItem();
                    lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    lstItem.Tag = fileName;
                    tracklist.Items.Add(lstItem);
                }
            }
            var total_playlist = tracklist.Items.Count;
            shuffle = new int[total_playlist];
            for (int i = 0; i != total_playlist; i++)
            {
                shuffle[i] = 0;
            }
            if (currentTrack == null) // Premier drag and drop
            {
                tracklist.SelectedIndex = 0;
                PlayTrack();
            }
        }

        private void PlayTrack()
        {
            mytabControl.SelectedIndex = 0;
            if (tracklist.SelectedItem != currentTrack)
            {
                try 
                { 
                    if (currentTrack != null)
                    {
                        PreviousTrack = currentTrack;
                        PreviousTrack.Foreground = TrackColor;
                    }
                    currentTrack = (ListBoxItem)tracklist.SelectedItem;
                    currentTrack.Foreground = currentTrackIndicator;
                    mPlayer.Source = new Uri(currentTrack.Tag.ToString());
                    SliderTimeLine.Value = 0;
                    mPlayer.LoadedBehavior = MediaState.Play;
                }
                catch { }
            }
            else
                mPlayer.LoadedBehavior = MediaState.Play;
        }

        private void MoveToNextTrack()
        {
            if (check_shuffle == false)
            {
                if (tracklist.Items.IndexOf(currentTrack) < tracklist.Items.Count - 1)
                {
                    tracklist.SelectedIndex = tracklist.Items.IndexOf(currentTrack) + 1;
                    PlayTrack();
                    return;
                }
                else if (tracklist.Items.IndexOf(currentTrack) == tracklist.Items.Count - 1)
                {
                    tracklist.SelectedIndex = 0;
                    PlayTrack();
                    return;
                }
            }
            else
            {
                int j = 0;
                var total_playlist = tracklist.Items.Count;

                for (int i = 0; i < total_playlist; i++)
                {
                    if (shuffle[i] == 0)
                        j += 1;
                }
                if (j == 0)
                {
                    for (int k = 0; k < total_playlist; k++)
                        shuffle[k] = 0;
                    j = total_playlist;
                }
                int[] tab = new int[j];
                for (int i = 0, k = 0; i < total_playlist; i++)
                {
                    if (shuffle[i] == 0)
                    {
                        tab[k] = i;
                        k++;
                    }
                }
                bool a = true;
                Random index = new Random();
                int test = 0;
                while (a)
                {
                    test = index.Next(0, total_playlist);
                    foreach (int key in tab)
                    {
                        if (key == test)
                            a = false;
                    }
                }
                tracklist.SelectedIndex = test;
                PreviousTrack = currentTrack;
                currentTrack = (ListBoxItem)tracklist.SelectedItem;
                currentTrack.Foreground = currentTrackIndicator;
                if (PreviousTrack != null)
                    PreviousTrack.Foreground = TrackColor;
                mPlayer.Source = new Uri(currentTrack.Tag.ToString());
                shuffle[test] = 1;
                mPlayer.LoadedBehavior = MediaState.Play;
            }
        }

        private void MoveToPrecedentTrack()
        {
            if (tracklist.Items.IndexOf(currentTrack) > 0)
            {
                tracklist.SelectedIndex = tracklist.Items.IndexOf(currentTrack) - 1;
                PlayTrack();
            }
            else if (tracklist.Items.IndexOf(currentTrack) == 0)
            {
                tracklist.SelectedIndex = tracklist.Items.Count - 1;
                PlayTrack();
            }

        }

        /*Bibliothèque*/

        private void DataGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
                e.Effects = DragDropEffects.None;
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Biblio>));
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string fileName in s)
            {
                //MessageBox.Show(fileName);
                string extension = System.IO.Path.GetExtension(fileName);
                if (extension.ToLowerInvariant() == ".mp3" || extension.ToLowerInvariant() == ".wav")
                {
                    ListBoxItem lstItem = new ListBoxItem();
                    lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    lstItem.Tag = fileName;
                    try
                    {
                        file = TagLib.File.Create(lstItem.Tag.ToString());
                        if (file.Tag.Title == null || file.Tag.FirstGenre == null)
                            liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = null, Path = fileName });
                        else
                            liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = file.Tag.Title, Genre = file.Tag.FirstGenre.ToString(), Path = fileName });   // si vide remplir avec le nom
                        using (StreamWriter wr = new StreamWriter("./bibliotheque.xml"))
                        {
                            xs.Serialize(wr, liste_bi);
                        }
                        Bibliotheque.ItemsSource = null;
                        Bibliotheque.ItemsSource = liste_bi;
                        Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
                    }
                    catch { }
                }                   
                else if (extension.ToLowerInvariant() == ".jpg" || extension.ToLowerInvariant() == ".avi" ||
                        extension.ToLowerInvariant() == ".jpeg" || extension.ToLowerInvariant() == ".png" ||
                        extension.ToLowerInvariant() == ".bmp" || extension.ToLowerInvariant() == ".wmv" ||
                        extension.ToLowerInvariant() == ".mp4")
                {
                    ListBoxItem lstItem = new ListBoxItem();
                    lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    lstItem.Tag = fileName;
                    file = TagLib.File.Create(lstItem.Tag.ToString());
                    liste_bi.Add(new Biblio() { Album = null, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = null, Path = fileName });
                    using (StreamWriter wr = new StreamWriter("./bibliotheque.xml"))
                    {
                        xs.Serialize(wr, liste_bi);
                    }
                    Bibliotheque.ItemsSource = null;
                    Bibliotheque.ItemsSource = liste_bi;
                    Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
                }
            }
        }

        private void menuitem_addfichier(object sender, RoutedEventArgs e)//Changement les fichier vont dans la bibliotèque !
        {
            WinForm::Form F = new Load();
            WinForm::OpenFileDialog folderDlg = new WinForm::OpenFileDialog();
            folderDlg.ShowReadOnly = true;
            WinForm::DialogResult result = folderDlg.ShowDialog();
            if (result == WinForm::DialogResult.OK)
            {
                fileName = folderDlg.InitialDirectory + folderDlg.FileName;
                try
                {
                    string extension = System.IO.Path.GetExtension(fileName);
                    if (extension.ToLowerInvariant() == ".mp3" || extension.ToLowerInvariant() == ".jpg" ||
                    extension.ToLowerInvariant() == ".avi" || extension.ToLowerInvariant() == ".jpeg" ||
                    extension.ToLowerInvariant() == ".wav" || extension.ToLowerInvariant() == ".wmv" ||
                    extension.ToLowerInvariant() == ".png" || extension.ToLowerInvariant() == ".bmp" ||
                    extension.ToLowerInvariant() == ".mp4")
                    {
                        ListBoxItem lstItem = new ListBoxItem();
                        lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(fileName);
                        lstItem.Tag = fileName;
                        file = TagLib.File.Create(lstItem.Tag.ToString());
                        if (file.Tag.Title == null)
                            liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = file.Tag.FirstGenre.ToString(), Path = fileName });
                        else
                            liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = file.Tag.FirstGenre.ToString(), Path = fileName });   // si vide remplir avec le nom
                        Bibliotheque.ItemsSource = null;
                        Bibliotheque.ItemsSource= liste_bi;
                        Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
                    }
                }
                catch
                {
                }
            }
         }

        private void menuitem_addfolder(object sender, RoutedEventArgs e)
        {

        }
       
        /*Slider Time*/

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
            {
                SliderTimeLine.Value = mPlayer.Position.TotalSeconds;
                if (url_player.Source != null)
                {
                    SliderTimeLine.Value = url_player.Position.TotalSeconds;
                    urlplayer_time = url_player.Position.Hours.ToString() + ":" + url_player.Position.Minutes.ToString() + ":" + url_player.Position.Seconds.ToString();
                    time.Text = urlplayer_time;
                }
                mPlayer_time = mPlayer.Position.Hours.ToString() + ":" + mPlayer.Position.Minutes.ToString() + ":" + mPlayer.Position.Seconds.ToString();
                time.Text = mPlayer_time;
            }
        }

        private void SliderTimeLine_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isDragging = true;
        }

        private void SliderTimeLine_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            isDragging = false;
            mPlayer.Position = TimeSpan.FromSeconds(SliderTimeLine.Value);
            url_player.Position = TimeSpan.FromSeconds(SliderTimeLine.Value);
        }

        private void SliderTimeLine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mPlayer.Position = TimeSpan.FromSeconds(SliderTimeLine.Value);
            url_player.Position = TimeSpan.FromSeconds(SliderTimeLine.Value);
        }

        /* Boutton */

        private void mybtn_stop(object sender, RoutedEventArgs e)
        {
            url_player.LoadedBehavior = MediaState.Stop;
            SliderTimeLine.Value = 0;
            mPlayer.LoadedBehavior = MediaState.Stop;
        }

        private void mybtn_next(object sender, RoutedEventArgs e)
        {
            if (tracklist.HasItems)
            {
                MoveToNextTrack();
            }
        }

        private void mybtn_play(object sender, RoutedEventArgs e)
        {
            if (tracklist.HasItems && mytabControl.SelectedIndex == 0)
            {
                PlayTrack();
            }
            else
                MessageBox.Show("Aucun titre dans la liste de lecture", "Attention");
            url_player.LoadedBehavior = MediaState.Play;
        }

        private void mybtn_pause(object sender, RoutedEventArgs e)
        {
            if (tracklist.HasItems)
            {
                mPlayer.LoadedBehavior = MediaState.Pause;
            }
        }

        private void mybtn_prev(object sender, RoutedEventArgs e)
        {
            if (tracklist.HasItems)
            {
                MoveToPrecedentTrack();
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (check_mute == false)
            {
                save_vol = SliderVolume.Value;
                SliderVolume.Value = 0;
                check_mute = true;
            }
            else
            {
                SliderVolume.Value = save_vol;
                check_mute = false;
            }
        }

        private void mybtn_go(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textblock_url.Text != null)
                {
                    url_player.Source = new Uri(textbox_url.Text, UriKind.Absolute);
                    url_player.LoadedBehavior = MediaState.Play;
                }
            }
            catch
            { }
        }

        private void mybtn_clean(object sender, RoutedEventArgs e)
        {
            tracklist.Items.Clear();
            mPlayer.LoadedBehavior = MediaState.Stop;
        }

        private void mybtn_save(object sender, RoutedEventArgs e)
        {
            WinForm::Form f = new Save();
            WinForm::SaveFileDialog saveDlg = new WinForm.SaveFileDialog();
            WinForm::DialogResult result = saveDlg.ShowDialog();
            if (result == WinForm.DialogResult.OK)
            {
                if (tracklist.Items.Count > 0)
                {
                    XmlSerializer xs = new XmlSerializer(typeof(List<string>));
                    using (StreamWriter wr = new StreamWriter(saveDlg.FileName + ".xml"))
                    {
                        List<string> test = new List<string>();
                        foreach (ListBoxItem itemText in tracklist.Items)
                        {
                            test.Add(itemText.Tag.ToString());
                        }
                        xs.Serialize(wr, test);
                        wr.Close();
                        MessageBox.Show("Success");
                    }
                }
            }

        }

        private void mybtn_load(object sender, RoutedEventArgs e)
        {
            WinForm::Form F = new Load();
            WinForm::OpenFileDialog folderDlg = new WinForm::OpenFileDialog();
            folderDlg.ShowReadOnly = true;
            // Show the FolderBrowserDialog.
            WinForm::DialogResult result = folderDlg.ShowDialog();
            if (result == WinForm::DialogResult.OK)
            {
                F.Text = folderDlg.InitialDirectory + folderDlg.FileName;
            }
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                StreamReader sr = new StreamReader(F.Text);
                if (System.IO.Path.GetExtension(F.Text) == ".xml")
                {
                    XmlSerializer xs = new XmlSerializer(typeof(List<string>));
                    List<string> test = new List<string>();
                    try
                    {
                        using (StreamReader rd = new StreamReader(F.Text))
                        {
                            try
                            {
                                test = xs.Deserialize(rd) as List<string>;
                            }
                            catch(System.InvalidOperationException d)
                            {
                                MessageBox.Show("Le fichier .xml que vous essayer d'ouvrir est endommagé", "Erreur");
                            }
                        }
                        foreach (String zik in test)
                        {
                            ListBoxItem lstItem = new ListBoxItem();
                            lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(zik);
                            lstItem.Tag = zik;
                            tracklist.Items.Add(lstItem);
                        }
                    }
                    finally
                    {
                        sr.Close();
                    }
                }
                else
                    MessageBox.Show("Format de fichier incorect");
            }
            catch
            {
            }
            var total_playlist = tracklist.Items.Count;
            shuffle = new int[total_playlist];
            for (int i = 0; i != total_playlist; i++)
            {
                shuffle[i] = 0;
            }
        }

        private void mybtn_repeat(object sender, RoutedEventArgs e)
        {
            if (check_repeat == false)
                check_repeat = true;
            else
                check_repeat = false;
        }

        private void mybtn_shuffle(object sender, RoutedEventArgs e)
        {
            if (check_shuffle == false)
                check_shuffle = true;
            else
                check_shuffle = false;

        }
        
        /*Double Click*/

        private void tracklist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            mytabControl.SelectedIndex = 0;
            PlayTrack();
        }

        private void biblio_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Bibliotheque.SelectedItems != null && mytabControl.SelectedIndex == 1)
            {
                int i = Bibliotheque.SelectedIndex;
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(liste_bi[i].Path);
                lstItem.Tag = liste_bi[i].Path;
                tracklist.Items.Add(lstItem);
                var total_playlist = tracklist.Items.Count;
                shuffle = new int[total_playlist];
                for (int j = 0; j != total_playlist; j++)
                {
                    shuffle[j] = 0;
                }
                PlayTrack();
            }
        }

        private void tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeView tree = (TreeView)sender;
            TreeViewItem temp = ((TreeViewItem)tree.SelectedItem);

            if (temp == null)
                return;
            SelectedImagePath = "";
            string temp1 = "";
            string temp2 = "";
            while (true)
            {
                temp1 = temp.Header.ToString();
                if (temp1.Contains(@"\"))
                {
                    temp2 = "";
                }
                SelectedImagePath = temp1 + temp2 + SelectedImagePath;
                if (temp.Parent.GetType().Equals(typeof(TreeView)))
                {
                    break;
                }
                temp = ((TreeViewItem)temp.Parent);
                temp2 = @"\";
            }
            add_to_tree();
        }

        private void add_to_tree()
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Biblio>));
            string extension = System.IO.Path.GetExtension(SelectedImagePath);
            if (extension.ToLowerInvariant() == ".mp3" || extension.ToLowerInvariant() == ".wav")
            {
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(SelectedImagePath);
                lstItem.Tag = SelectedImagePath;
                file = TagLib.File.Create(lstItem.Tag.ToString());
                if (file.Tag.Title == null || file.Tag.FirstGenre == null)
                    liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = null, Path = SelectedImagePath });
                else
                    liste_bi.Add(new Biblio() { Album = file.Tag.Album, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = file.Tag.Title, Genre = file.Tag.FirstGenre.ToString(), Path = SelectedImagePath });
                using (StreamWriter wr = new StreamWriter("./bibliotheque.xml"))
                {
                    xs.Serialize(wr, liste_bi);
                }
                Bibliotheque.ItemsSource = null;
                Bibliotheque.ItemsSource = liste_bi;
                Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
            }
            else if (extension.ToLowerInvariant() == ".jpg" || extension.ToLowerInvariant() == ".avi" ||
                    extension.ToLowerInvariant() == ".jpeg" || extension.ToLowerInvariant() == ".png" ||
                    extension.ToLowerInvariant() == ".bmp" || extension.ToLowerInvariant() == ".wmv" ||
                    extension.ToLowerInvariant() == ".mp4")
            {
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(SelectedImagePath);
                lstItem.Tag = SelectedImagePath;
                file = TagLib.File.Create(lstItem.Tag.ToString());
                liste_bi.Add(new Biblio() { Album = null, Duree = file.Properties.Duration.Hours.ToString() + ":" + file.Properties.Duration.Minutes.ToString() + ":" + file.Properties.Duration.Seconds.ToString(), Titre = lstItem.Content.ToString(), Genre = null, Path = SelectedImagePath });
                using (StreamWriter wr = new StreamWriter("./bibliotheque.xml"))
                {
                    xs.Serialize(wr, liste_bi);
                }
                Bibliotheque.ItemsSource = null;
                Bibliotheque.ItemsSource = liste_bi;
                Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
            }
        }

        private void delete_click(object sender, RoutedEventArgs e)
        {
            XmlSerializer xs = new XmlSerializer(typeof(List<Biblio>));
            try
            {
               for (int i = 0; i < Bibliotheque.SelectedItems.Count; i++)
               {
                   Biblio suppr = Bibliotheque.SelectedItems[i] as Biblio;
                   liste_bi.Remove(suppr);
               }
            }
            catch { }
            Bibliotheque.ItemsSource = null;
            Bibliotheque.ItemsSource = liste_bi;
            using (StreamWriter wr = new StreamWriter("./bibliotheque.xml"))
            {
                xs.Serialize(wr, liste_bi);
            }
        }

        private void add_click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Bibliotheque.SelectedItems.Count; i++)
            {
                Biblio add = Bibliotheque.SelectedItems[i] as Biblio;
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Content = System.IO.Path.GetFileNameWithoutExtension(add.Path);
                lstItem.Tag = add.Path;
                tracklist.Items.Add(lstItem);
                var total_playlist = tracklist.Items.Count;
                shuffle = new int[total_playlist];
                for (int j = 0; j != total_playlist; j++)
                {
                    shuffle[j] = 0;
                }
                PlayTrack();
            }
        }

        private void find_click(object sender, RoutedEventArgs e)
        {
            string search = find.Text.ToLower();
            Regex MyRegex = new Regex("^.*" + search + ".*$");
            bool ok = false;
            int i = 0;
            List<Biblio> tmp = new List<Biblio>();
            for (i = 0; i < liste_bi.Count; i++)
            {
                try
                {
                    bool album = false;
                    bool titre = false;
                    bool genre = false;
                    if (liste_bi[i].Album != null)
                    {
                        Match m = MyRegex.Match(liste_bi[i].Album.ToLower());
                         album = m.Success;
                       }
                    if (liste_bi[i].Titre != null)
                    {
                        Match m2 = MyRegex.Match(liste_bi[i].Titre.ToLower());
                        titre = m2.Success;
                    }
                    if (liste_bi[i].Genre != null)
                    {
                        Match m3 = MyRegex.Match(liste_bi[i].Genre.ToLower());
                        genre = m3.Success;
                    }
                    if (album || titre || genre)
                    {
                        tmp.Add(new Biblio() { Album = liste_bi[i].Album, Duree = liste_bi[i].Duree, Genre = liste_bi[i].Genre, Path = liste_bi[i].Path, Titre = liste_bi[i].Titre });
                        Bibliotheque.ItemsSource = null;
                        Bibliotheque.ItemsSource = tmp;
                        ok = true;
                        Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
                    }
                }
                catch
                {

                }
            }
            if (search == "" || (i == liste_bi.Count && ok == false))
            {
                Bibliotheque.ItemsSource = null;
                Bibliotheque.ItemsSource = liste_bi;
                Bibliotheque.Columns[4].Visibility = Visibility.Hidden;
            }
        }
    }
}