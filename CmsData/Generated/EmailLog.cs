using System; 
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;

namespace CmsData
{
	[Table(Name="dbo.EmailLog")]
	public partial class EmailLog : INotifyPropertyChanging, INotifyPropertyChanged
	{
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
	#region Private Fields
		
		private int _Id;
		
		private string _Fromaddr;
		
		private string _Toaddr;
		
		private DateTime? _Time;
		
		private string _Subject;
		
   		
    	
	#endregion
	
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
		
		partial void OnIdChanging(int value);
		partial void OnIdChanged();
		
		partial void OnFromaddrChanging(string value);
		partial void OnFromaddrChanged();
		
		partial void OnToaddrChanging(string value);
		partial void OnToaddrChanged();
		
		partial void OnTimeChanging(DateTime? value);
		partial void OnTimeChanged();
		
		partial void OnSubjectChanging(string value);
		partial void OnSubjectChanged();
		
    #endregion
		public EmailLog()
		{
			
			
			OnCreated();
		}

		
    #region Columns
		
		[Column(Name="id", UpdateCheck=UpdateCheck.Never, Storage="_Id", AutoSync=AutoSync.OnInsert, DbType="int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int Id
		{
			get { return this._Id; }

			set
			{
				if (this._Id != value)
				{
				
                    this.OnIdChanging(value);
					this.SendPropertyChanging();
					this._Id = value;
					this.SendPropertyChanged("Id");
					this.OnIdChanged();
				}

			}

		}

		
		[Column(Name="fromaddr", UpdateCheck=UpdateCheck.Never, Storage="_Fromaddr", DbType="varchar(50)")]
		public string Fromaddr
		{
			get { return this._Fromaddr; }

			set
			{
				if (this._Fromaddr != value)
				{
				
                    this.OnFromaddrChanging(value);
					this.SendPropertyChanging();
					this._Fromaddr = value;
					this.SendPropertyChanged("Fromaddr");
					this.OnFromaddrChanged();
				}

			}

		}

		
		[Column(Name="toaddr", UpdateCheck=UpdateCheck.Never, Storage="_Toaddr", DbType="varchar(150)")]
		public string Toaddr
		{
			get { return this._Toaddr; }

			set
			{
				if (this._Toaddr != value)
				{
				
                    this.OnToaddrChanging(value);
					this.SendPropertyChanging();
					this._Toaddr = value;
					this.SendPropertyChanged("Toaddr");
					this.OnToaddrChanged();
				}

			}

		}

		
		[Column(Name="time", UpdateCheck=UpdateCheck.Never, Storage="_Time", DbType="datetime")]
		public DateTime? Time
		{
			get { return this._Time; }

			set
			{
				if (this._Time != value)
				{
				
                    this.OnTimeChanging(value);
					this.SendPropertyChanging();
					this._Time = value;
					this.SendPropertyChanged("Time");
					this.OnTimeChanged();
				}

			}

		}

		
		[Column(Name="subject", UpdateCheck=UpdateCheck.Never, Storage="_Subject", DbType="varchar(180)")]
		public string Subject
		{
			get { return this._Subject; }

			set
			{
				if (this._Subject != value)
				{
				
                    this.OnSubjectChanging(value);
					this.SendPropertyChanging();
					this._Subject = value;
					this.SendPropertyChanged("Subject");
					this.OnSubjectChanged();
				}

			}

		}

		
    #endregion
        
    #region Foreign Key Tables
   		
	#endregion
	
	#region Foreign Keys
    	
	#endregion
	
		public event PropertyChangingEventHandler PropertyChanging;
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
				this.PropertyChanging(this, emptyChangingEventArgs);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

   		
	}

}

