using System; 
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;

namespace CmsData.View
{
	[Table(Name="RecentAbsents")]
	public partial class RecentAbsent
	{
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		
		private int _OrganizationId;
		
		private string _OrganizationName;
		
		private string _LeaderName;
		
		private int? _Consecutive;
		
		private int _PeopleId;
		
		private string _Name2;
		
		private string _HomePhone;
		
		private string _CellPhone;
		
		private string _EmailAddress;
		
		private DateTime? _Lastmeeting;
		
		private int _MeetingId;
		
		private int? _ConsecutiveAbsentsThreshold;
		
		
		public RecentAbsent()
		{
		}

		
		
		[Column(Name="OrganizationId", Storage="_OrganizationId", DbType="int NOT NULL")]
		public int OrganizationId
		{
			get
			{
				return this._OrganizationId;
			}

			set
			{
				if (this._OrganizationId != value)
					this._OrganizationId = value;
			}

		}

		
		[Column(Name="OrganizationName", Storage="_OrganizationName", DbType="varchar(100) NOT NULL")]
		public string OrganizationName
		{
			get
			{
				return this._OrganizationName;
			}

			set
			{
				if (this._OrganizationName != value)
					this._OrganizationName = value;
			}

		}

		
		[Column(Name="LeaderName", Storage="_LeaderName", DbType="varchar(50)")]
		public string LeaderName
		{
			get
			{
				return this._LeaderName;
			}

			set
			{
				if (this._LeaderName != value)
					this._LeaderName = value;
			}

		}

		
		[Column(Name="consecutive", Storage="_Consecutive", DbType="int")]
		public int? Consecutive
		{
			get
			{
				return this._Consecutive;
			}

			set
			{
				if (this._Consecutive != value)
					this._Consecutive = value;
			}

		}

		
		[Column(Name="PeopleId", Storage="_PeopleId", DbType="int NOT NULL")]
		public int PeopleId
		{
			get
			{
				return this._PeopleId;
			}

			set
			{
				if (this._PeopleId != value)
					this._PeopleId = value;
			}

		}

		
		[Column(Name="Name2", Storage="_Name2", DbType="varchar(139)")]
		public string Name2
		{
			get
			{
				return this._Name2;
			}

			set
			{
				if (this._Name2 != value)
					this._Name2 = value;
			}

		}

		
		[Column(Name="HomePhone", Storage="_HomePhone", DbType="varchar(20)")]
		public string HomePhone
		{
			get
			{
				return this._HomePhone;
			}

			set
			{
				if (this._HomePhone != value)
					this._HomePhone = value;
			}

		}

		
		[Column(Name="CellPhone", Storage="_CellPhone", DbType="varchar(20)")]
		public string CellPhone
		{
			get
			{
				return this._CellPhone;
			}

			set
			{
				if (this._CellPhone != value)
					this._CellPhone = value;
			}

		}

		
		[Column(Name="EmailAddress", Storage="_EmailAddress", DbType="varchar(150)")]
		public string EmailAddress
		{
			get
			{
				return this._EmailAddress;
			}

			set
			{
				if (this._EmailAddress != value)
					this._EmailAddress = value;
			}

		}

		
		[Column(Name="lastmeeting", Storage="_Lastmeeting", DbType="datetime")]
		public DateTime? Lastmeeting
		{
			get
			{
				return this._Lastmeeting;
			}

			set
			{
				if (this._Lastmeeting != value)
					this._Lastmeeting = value;
			}

		}

		
		[Column(Name="MeetingId", Storage="_MeetingId", DbType="int NOT NULL")]
		public int MeetingId
		{
			get
			{
				return this._MeetingId;
			}

			set
			{
				if (this._MeetingId != value)
					this._MeetingId = value;
			}

		}

		
		[Column(Name="ConsecutiveAbsentsThreshold", Storage="_ConsecutiveAbsentsThreshold", DbType="int")]
		public int? ConsecutiveAbsentsThreshold
		{
			get
			{
				return this._ConsecutiveAbsentsThreshold;
			}

			set
			{
				if (this._ConsecutiveAbsentsThreshold != value)
					this._ConsecutiveAbsentsThreshold = value;
			}

		}

		
    }

}
