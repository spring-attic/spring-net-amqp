using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spring.Messaging.Amqp.Tests.Support.Converter
{
    public class SimpleTrade
    {
        private string ticker;

        private ulong quantity;

        private decimal price;

        private string orderType;

        private string accountName;

        private bool buyRequest;

        private string userName;

        private string requestId;

        public string Ticker
        {
            get
            {
                return this.ticker;
            }
            set
            {
                this.ticker = value;
            }
        }

        public ulong Quantity
        {
            get
            {
                return this.quantity;
            }
            set
            {
                this.quantity = value;
            }
        }

        public decimal Price
        {
            get
            {
                return this.price;
            }
            set
            {
                this.price = value;
            }
        }

        public string OrderType
        {
            get
            {
                return this.orderType;
            }
            set
            {
                this.orderType = value;
            }
        }

        public string AccountName
        {
            get
            {
                return this.accountName;
            }
            set
            {
                this.accountName = value;
            }
        }


        public bool IsBuyRequest
        {
            get
            {
                return this.buyRequest;
            }
            set
            {
                this.buyRequest = value;
            }
        }

        public string UserName
        {
            get
            {
                return this.userName;
            }
            set
            {
                this.userName = value;
            }
        }

        public string RequestId
        {
            get
            {
                return this.requestId;
            }
            set
            {
                this.requestId = value;
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            var result = 1;
            result = prime * result + ((accountName == null) ? 0 : accountName.GetHashCode());
            result = prime * result + (buyRequest ? 1231 : 1237);
            result = prime * result + ((orderType == null) ? 0 : orderType.GetHashCode());
            result = prime * result + ((price == null) ? 0 : price.GetHashCode());
            result = prime * result + (int)(quantity ^ (quantity >> 32));
            result = prime * result + ((requestId == null) ? 0 : requestId.GetHashCode());
            result = prime * result + ((ticker == null) ? 0 : ticker.GetHashCode());
            result = prime * result + ((userName == null) ? 0 : userName.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (typeof(SimpleTrade) != obj.GetType()) return false;

            SimpleTrade other = (SimpleTrade)obj;
            if (accountName == null)
            {
                if (other.accountName != null) return false;
            }
            else if (!accountName.Equals(other.accountName)) return false;
            if (buyRequest != other.buyRequest) return false;
            if (orderType == null)
            {
                if (other.orderType != null) return false;
            }
            else if (!orderType.Equals(other.orderType)) return false;
            if (price == null)
            {
                if (other.price != null) return false;
            }
            else if (!price.Equals(other.price)) return false;
            if (quantity != other.quantity) return false;
            if (requestId == null)
            {
                if (other.requestId != null) return false;
            }
            else if (!requestId.Equals(other.requestId)) return false;
            if (ticker == null)
            {
                if (other.ticker != null) return false;
            }
            else if (!ticker.Equals(other.ticker)) return false;
            if (userName == null)
            {
                if (other.userName != null) return false;
            }
            else if (!userName.Equals(other.userName)) return false;
            return true;
        }
    }
}
