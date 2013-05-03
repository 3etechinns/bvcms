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
	[Table(Name="dbo.EmailQueueToFail")]
	public partial class EmailQueueToFail : INotifyPropertyChanging, INotifyPropertyChanged
	{
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
	#region Private Fields
		
		private int? _Id;
		
		private int? _PeopleId;
		
		private DateTime? _Time;
		
		private string _EventX;
		
		private string _Reason;
		
		private string _Bouncetype;
		
		private string _Email;
		
		private int _Pkey;
		
		private long? _Timestamp;
		
   		
    	
	#endregion
	
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
		
		partial void OnIdChanging(int? value);
		partial void OnIdChanged();
		
		partial void OnPeopleIdChanging(int? value);
		partial void OnPeopleIdChanged();
		
		partial void OnTimeChanging(DateTime? value);
		partial void OnTimeChanged();
		
		partial void OnEventXChanging(string value);
		partial void OnEventXChanged();
		
		partial void OnReasonChanging(string value);
		partial void OnReasonChanged();
		
		partial void OnBouncetypeChanging(string value);
		partial void OnBouncetypeChanged();
		
		partial void OnEmailChanging(string value);
		partial void OnEmailChanged();
		
		partial void OnPkeyChanging(int value);
		partial void OnPkeyChanged();
		
		partial void OnTimestampChanging(long? value);
		partial void OnTimestampChanged();
		
    #endregion
		public EmailQueueToFail()
		{
			
			
			OnCreated();
		}

		
    #region Columns
		
		[Column(Name="Id", UpdateCheck=UpdateCheck.Never, Storage="_Id", DbType="int")]
		public int? Id
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

		
		[Column(Name="PeopleId", UpdateCheck=UpdateCheck.Never, Storage="_PeopleId", DbType="int")]
		public int? PeopleId
		{
			get { return this._PeopleId; }

			set
			{
				if (this._PeopleId != value)
				{
				
                    this.OnPeopleIdChanging(value);
					this.SendPropertyChanging();
					this._PeopleId = value;
					this.SendPropertyChanged("PeopleId");
					this.OnPeopleIdChanged();
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

		
		[Column(Name="event", UpdateCheck=UpdateCheck.Never, Storage="_EventX", DbType="varchar(20)")]
		public string EventX
		{
			get { return this._EventX; }

			set
			{
				if (this._EventX != value)
				{
				
                    this.OnEventXChanging(value);
					this.SendPropertyChanging();
					this._EventX = value;
					this.SendPropertyChanged("EventX");
					this.OnEventXChanged();
				}

			}

		}

		
		[Column(Name="reason", UpdateCheck=UpdateCheck.Never, Storage="_Reason", DbType="varchar(300)")]
		public string Reason
		{
			get { return this._Reason; }

			set
			{
				if (this._Reason != value)
				{
				
                    this.OnReasonChanging(value);
					this.SendPropertyChanging();
					this._Reason = value;
					this.SendPropertyChanged("Reason");
					this.OnReasonChanged();
				}

			}

		}

		
		[Column(Name="bouncetype", UpdateCheck=UpdateCheck.Never, Storage="_Bouncetype", DbType="varchar(20)")]
		public string Bouncetype
		{
			get { return this._Bouncetype; }

			set
			{
				if (this._Bouncetype != value)
				{
				
                    this.OnBouncetypeChanging(value);
					this.SendPropertyChanging();
					this._Bouncetype = value;
					this.SendPropertyChanged("Bouncetype");
					this.OnBouncetypeChanged();
				}

			}

		}

		
		[Column(Name="email", UpdateCheck=UpdateCheck.Never, Storage="_Email", DbType="varchar(100)")]
		public string Email
		{
			get { return this._Email; }

			set
			{
				if (this._Email != value)
				{
				
                    this.OnEmailChanging(value);
					this.SendPropertyChanging();
					this._Email = value;
					this.SendPropertyChanged("Email");
					this.OnEmailChanged();
				}

			}

		}

		
		[Column(Name="pkey", UpdateCheck=UpdateCheck.Never, Storage="_Pkey", AutoSync=AutoSync.OnInsert, DbType="int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int Pkey
		{
			get { return this._Pkey; }

			set
			{
				if (this._Pkey != value)
				{
				
                    this.OnPkeyChanging(value);
					this.SendPropertyChanging();
					this._Pkey = value;
					this.SendPropertyChanged("Pkey");
					this.OnPkeyChanged();
				}

			}

		}

		
		[Column(Name="timestamp", UpdateCheck=UpdateCheck.Never, Storage="_Timestamp", DbType="bigint")]
		public long? Timestamp
		{
			get { return this._Timestamp; }

			set
			{
				if (this._Timestamp != value)
				{
				
                    this.OnTimestampChanging(value);
					this.SendPropertyChanging();
					this._Timestamp = value;
					this.SendPropertyChanged("Timestamp");
					this.OnTimestampChanged();
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

