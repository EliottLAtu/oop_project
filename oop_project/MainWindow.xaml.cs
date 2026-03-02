using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Ical.Net;
using System.Net.Http;
using System.ComponentModel;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using System.Globalization;
using System.Data.Entity;


namespace oop_project
{

    public class user
    {
        public int Id = 1;
        public string IcalUrl { get; set; }
        public virtual List<Evenement> Evenements { get; set; } = new List<Evenement>();

        public List<work> tasks { get; set; } = new List<work>();

        public override string ToString()
        {
            //still to write
            // to call when saving to json format 
            return base.ToString();
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
    }

    public class work
    {
        public int userId = 1;
        public virtual user User { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }  
    }

    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
            using (db)
            {
                db.Users.Add(actu);
                db.SaveChanges();
            }

        }
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public async Task ChargerCalendrierAsync()
        {
            //https://timetables.atu.ie/Ical/StudentSet?studentSetID=a6c7b613-634c-ddbc-984c-ca7f1e7cc858&locality=sligo 
            if (string.IsNullOrWhiteSpace(UrlIcal))
                return;

            var client = new HttpClient();
            var ics = await client.GetStringAsync(UrlIcal);

            var calendar = Ical.Net.Calendar.Load(ics);

            Evenements.Clear();

            var start = new CalDateTime(DateTime.Today.AddMonths(-1));
            var end = new CalDateTime(DateTime.Today.AddMonths(2));

            var occurrences = calendar.Events
                .SelectMany(e => e.GetOccurrences(start, null));

            using (db)
            {
                foreach (var occ in occurrences)
                {
                    Evenements.Add(new Evenement
                    {
                        Titre = occ.Source.ToString(),
                        Debut = occ.Period.StartTime.ToTimeZone(TimeZone.CurrentTimeZone.ToString()).Value,
                        Fin = occ.Period.EndTime.ToTimeZone(TimeZone.CurrentTimeZone.ToString()).Value,
                        userId = 1,
                        User = actu,
                        Id = count++

                    });
                    db.Evenements.Add(Evenements.Last());
                }
                db.SaveChanges();
            }
            actu.Evenements = Evenements.ToList();
            OnPropertyChanged(nameof(EvenementsDuJour));


        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await ChargerCalendrierAsync();
        }

        private void btnTaskadd(object sender, RoutedEventArgs e)
        {
            using (db)
            {
                actu.tasks.Add(new work
                {
                    Name = tblk_task_name.Text,
                    Description = tblk_task_desc.Text,
                    Deadline = DateTime.ParseExact(tblk_task_due_date.Text, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    userId = 1,
                    User = actu,
                    Id = actu.tasks.Count + 1
                });
                db.Tasks.Add(actu.tasks.Last());
            }
            db.SaveChanges();

        }

        private void btnTaskDel(object sender, RoutedEventArgs e)
        {
            var rem = lstTasks.SelectedItem;
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

