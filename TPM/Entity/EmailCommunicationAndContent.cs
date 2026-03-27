using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TPM.Entity
{
    public class EmailCommunication
    {
        public string EmailTo { get; set; }
        public string StandardMailSubject { get; set; }
        public string StandardMailBody { get; set; }
        public string EmailBCC { get; set; }
        public string EmailCC { get; set; }
        public string ProposalMailSubject { get; set; }
        public string ProposalMailBody { get; set; }
        public string EmailConfirmText { get; set; }
        public string SMSConfirmText { get; set; }
        public string EmailAckText { get; set; }
        public string SMSAckText { get; set; }
        public string InvoiceMailSubject { get; set; }
        public string InvoiceMailBody { get; set; }
        public string AttachmentsName { get; set; }
        public string EmailType { get; set; }
        public decimal ReqDepoAmt { get; set; }
        public string RescheduledMailSubject { get; set; }
        public string RescheduleMailBody { get; set; }
        public List<EmailContent> EmailContents { get; set; }
    }

    public class EmailContent
    {
        public byte[] FileContent { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileUrl { get; set; }
    }
}