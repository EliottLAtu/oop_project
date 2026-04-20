using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
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
using Ical.Net;
using System.Net.Http;
using System.ComponentModel;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using System.Globalization;
using System.Data.Entity;
using PublicHoliday;
using System.Collections.Specialized;

namespace oop_project
{

    public class user
    {
        // variables : 

        public int UserId { get; set; }
        public string IcalUrl { get; set; }
        public virtual List<Evenement> Evenements { get; set; } = new List<Evenement>();

        public ObservableCollection<work> tasks { get; set; } = new ObservableCollection<work>();

        //save :
        public override string ToString()
        {
            
            string info = "";
            info += $"User ID: {UserId},\n";
            info += $"Ical URL: {IcalUrl},\n";
            info += "Evenements: {\n";
            foreach (var evt in Evenements)
            {
                info += $"Id: {evt.Id} {{ : \n  Titre: {evt.Titre},\n Debut: {evt.Debut},\n Fin: {evt.Fin}\n}}\n";
            }
            info += "}\n";
            info += "Tasks: {\n";
            foreach (var task in tasks)
            {
                info += $"Id: {task.Id} {{ : \n  Name: {task.Name},\n Description: {task.Description},\n Deadline: {task.Deadline}\n}}\n";
            }
            info += "}\n";


            return info;
        }
    }
    public class Evenement
    {
        public int userId = 1;
        public virtual user User { get; set; }
        
        public int Id { get; set; }
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
        public string Titre { get; set; }

        public DateTime DateUniquement => Debut.Date;
    }

    public class work
    {
        public int userId = 1;
        public virtual user User { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }

        public override string ToString()
        {
            string rep = Name+","+Description+","+Deadline.ToString();


            return rep;
        }
    }

    public static class HolidayService
    {
        public static IEnumerable<KeyValuePair<DateTime, string>> GetHolidaysForCurrentRegion(int year)
        {
            string countryCode = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            IPublicHolidays calculator;
            switch (countryCode)
            {
                case "FR":
                    calculator = new FrancePublicHoliday();
                    break;
                case "BE":
                    calculator =  new BelgiumPublicHoliday();
                    break;
                case "CA":
                    calculator= new CanadaPublicHoliday();
                    break;
                case "US":
                    calculator = new USAPublicHoliday();
                    break;
                case "DE":
                    calculator = new GermanPublicHoliday();
                    break;
                case "IE":
                    calculator = new IrelandPublicHoliday();
                    break;
                default:
                    calculator = new FrancePublicHoliday(); 
                    break;
            }
            return calculator.PublicHolidaysInformation(year).Select(h => new KeyValuePair<DateTime, string>(h.HolidayDate, h.Name));
        }
           
        }

    // <summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // variables :

        public string UrlIcal { get; set; }
        public ObservableCollection<Evenement> Evenements { get; } = new ObservableCollection<Evenement>();
        public DateTime JourSelectionne { get; set; } = DateTime.Today;
        public event PropertyChangedEventHandler PropertyChanged;
        user actu = new user();
        public int count = 1;
        datacontext db = new datacontext();
        public IEnumerable<Evenement> EvenementsDuJour => Evenements.Where(e => e.Debut.Date == JourSelectionne.Date).OrderBy(e => e.Debut);




        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            actu.UserId = 1;
            db.Users.Add(actu);
            db.SaveChanges();
            lstTasks.ItemsSource = actu.tasks;
            txtTaskDetails.Visibility = Visibility.Collapsed;
            Hollidays();
            Main_text.Text = "Hello, \n" +
                "This app is student made for a project using WPF \n" +
                "This app is a student planner to help you organize yourself better during college. \n" +
                "I hope it will help you well\n"+
                "If you have any improvements don't hesitate and contact me";


        }
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //loading calendar :
        public async Task ChargerCalendrierAsync()
        {
            //https://timetables.atu.ie/Ical/StudentSet?studentSetID=a6c7b613-634c-ddbc-984c-ca7f1e7cc858&locality=sligo 
            if (string.IsNullOrWhiteSpace(UrlIcal) && (actu.IcalUrl==null))
                return;

            var client = new HttpClient();
            var ics = await client.GetStringAsync(UrlIcal==null ? actu.IcalUrl : UrlIcal );

            var calendar = Ical.Net.Calendar.Load(ics);

            Evenements.Clear();

            var start = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddMonths(-1), DateTimeKind.Unspecified));
            var end = new CalDateTime(DateTime.SpecifyKind(DateTime.Today.AddMonths(2), DateTimeKind.Unspecified));


            var occurrences = calendar.Events
                .SelectMany(e => e.GetOccurrences(start, null));


            foreach (var occ in occurrences)
            {
                var calendarEvent = occ.Source as Ical.Net.CalendarComponents.CalendarEvent;

                Evenements.Add(new Evenement
                {
                    Titre = calendarEvent?.Summary ?? "Sans titre",
                    Debut = occ.Period.StartTime.ToTimeZone( TimeZoneInfo.Local.Id).Value,
                    Fin = occ.Period.EndTime?.ToTimeZone( TimeZoneInfo.Local.Id).Value ?? DateTime.MaxValue,
                    userId = 1,
                    User = actu,
                    Id = count++
                });

                db.Evenements.Add(Evenements.Last());
            }

            db.SaveChanges();
                
            actu.Evenements = Evenements.ToList();
            OnPropertyChanged(nameof(EvenementsDuJour));


        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ChargerCalendrierAsync();
        }

        private void btnTaskadd(object sender, RoutedEventArgs e)
        {
            DateTime deadline;
            actu.tasks.Add(new work
            {
                Name = tblk_task_name.Text,
                Description = tblk_task_desc.Text,
                Deadline = DateTime.TryParseExact(tblk_task_due_date.Text,"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out deadline) ? deadline : DateTime.Now,
                userId = 1,
                User = actu,
                Id = actu.tasks.Count + 1
            });
            tblk_task_desc.Text = "";
            tblk_task_due_date.Text = "";
            tblk_task_name.Text = "";
            db.Tasks.Add(actu.tasks.Last());
            db.SaveChanges();
            Console.WriteLine(actu.tasks.Count.ToString());
            OnPropertyChanged("actu.tasks");
            Console.WriteLine(lstTasks.Items.Count.ToString());
        }

        private void btnTaskDel(object sender, RoutedEventArgs e)
        {
            var rem = lstTasks.SelectedItem;
        }

        private void BtnSave(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".json";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                FileInfo file = new FileInfo(filename);
                if (!file.Exists)
                {
                    using (StreamWriter savefile = file.CreateText())
                    {
                        savefile.WriteLine("Made by Eliott Lapicque for a school project");
                        savefile.WriteLine(actu.ToString());
                        Messagebox.Text = "File saved successfully";
                    }
                }
            }


        }

        private void btnLoad(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".json";
            dlg.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
            dlg.CheckFileExists = true;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                FileInfo file = new FileInfo(filename);
                using (StreamReader loadfile = file.OpenText())
                {
                    if (loadfile.ReadLine()!= "Made by Eliott Lapicque for a school project")
                    {
                        Messagebox.Text=("This file is not compatible with the application");

                    }
                    else
                    {
                        
                            loadfile.ReadLine();
                            string[] temp = loadfile.ReadLine().Split(':');
                            string ical = "";
                            for (int i = 1; i < temp.Length; i++)
                            {
                                ical += temp[i].Trim();
                            }
                            actu.IcalUrl = ical;
                            ChargerCalendrierAsync();
                            loadfile.ReadLine();
                            string line = loadfile.ReadLine();
                            while (line != "}")
                            {
                                string ID, Titre, Debut, Fin;
                                ID = line.Split(':')[1].Trim().TrimEnd(',');
                                Titre = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd(',');
                                Debut = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd(',');
                                Fin = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd('}').TrimEnd(',');
                                Evenement evt = new Evenement
                                {
                                    Id = int.Parse(ID),
                                    Titre = Titre,
                                    Debut = DateTime.Parse(Debut),
                                    Fin = DateTime.Parse(Fin),
                                    userId = 1,
                                    User = actu
                                };
                                actu.Evenements.Add(evt);
                                db.Evenements.Add(evt);
                            }
                            loadfile.ReadLine();
                            line = loadfile.ReadLine();
                            while (line != "}")
                            {
                                string ID, Name, description, DEadline;
                                ID = line.Split(':')[1].Trim().TrimEnd(',');
                                Name = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd(',');
                                description = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd(',');
                                DEadline = loadfile.ReadLine().Split(':')[1].Trim().TrimEnd('}').TrimEnd(',');
                                work evt = new work
                                {
                                    Id = int.Parse(ID),
                                    Name = Name,
                                    Description = description,
                                    Deadline = DateTime.Parse(DEadline),
                                    userId = 1,
                                    User = actu
                                };
                                actu.tasks.Add(evt);
                                db.Tasks.Add(evt);
                            }
                            db.Users.Add(actu);
                            db.SaveChanges();
                        

                    }
                }
            }
            Messagebox.Text = "File loaded successfully";
        }

        private void window_closed(object sender, EventArgs e)
        {

            db.Dispose();
        }


        private void task_selected(object sender, SelectionChangedEventArgs e)
        {
            txtTaskDetails.Visibility = Visibility.Visible;
            txtTaskDetails.ItemsSource = lstTasks.SelectedItem.ToString().Split(',');
        }

        protected void Hollidays ()
        {
            var year = DateTime.Now.Year;
            var list = HolidayService.GetHolidaysForCurrentRegion(year)
                .Select(h => new {Date = $"{h.Key.Day}/{h.Key.Month}/{h.Key.Year}", h.Value })
                .OrderBy(h => h.Date)
                .ToList();
            
            Holidays.ItemsSource = list;
        }
    }

    public class datacontext : DbContext
    {
        public datacontext() : base("Mydatacontext") { }
        public DbSet<user> Users { get; set; }
        
        public DbSet<Evenement> Evenements { get; set; }
        public DbSet<work> Tasks { get; set; }
    }
}

