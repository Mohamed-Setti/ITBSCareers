using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Models.Carriere;

public partial class CarriereDbContext
{
    public virtual DbSet<ForumCategory> ForumCategories { get; set; }
    public virtual DbSet<ForumTopic> ForumTopics { get; set; }
    public virtual DbSet<ForumComment> ForumComments { get; set; }
    public virtual DbSet<ForumVote> ForumVotes { get; set; }
    public virtual DbSet<ForumReport> ForumReports { get; set; }
    public virtual DbSet<ForumTopicHistory> ForumTopicHistories { get; set; }
    public virtual DbSet<ForumCommentHistory> ForumCommentHistories { get; set; }
    public virtual DbSet<ForumUserBan> ForumUserBans { get; set; }

    public virtual DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public virtual DbSet<PrivateUserBlock> PrivateUserBlocks { get; set; }
    public virtual DbSet<PrivateUserReport> PrivateUserReports { get; set; }
    public virtual DbSet<MentorshipRequest> MentorshipRequests { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForumCategory>(entity =>
        {
            entity.ToTable("ForumCategories");
            entity.HasKey(e => e.ForumCategoryId);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<ForumTopic>(entity =>
        {
            entity.ToTable("ForumTopics");
            entity.HasKey(e => e.ForumTopicId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpvotesCount).HasDefaultValue(0);
            entity.Property(e => e.DownvotesCount).HasDefaultValue(0);
            entity.Property(e => e.CommentsCount).HasDefaultValue(0);
            entity.Property(e => e.ReportsCount).HasDefaultValue(0);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(e => e.ForumCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.ForumTopics)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumComment>(entity =>
        {
            entity.ToTable("ForumComments");
            entity.HasKey(e => e.ForumCommentId);
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpvotesCount).HasDefaultValue(0);
            entity.Property(e => e.DownvotesCount).HasDefaultValue(0);
            entity.Property(e => e.ReportsCount).HasDefaultValue(0);

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Comments)
                .HasForeignKey(e => e.ForumTopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.ForumComments)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumVote>(entity =>
        {
            entity.ToTable("ForumVotes");
            entity.HasKey(e => e.ForumVoteId);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsUpvote).HasDefaultValue(true);
            entity.HasIndex(e => new { e.ForumTopicId, e.UserId }).IsUnique();
            entity.HasIndex(e => new { e.ForumCommentId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Votes)
                .HasForeignKey(e => e.ForumTopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Comment)
                .WithMany()
                .HasForeignKey(e => e.ForumCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ForumVotes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumReport>(entity =>
        {
            entity.ToTable("ForumReports");
            entity.HasKey(e => e.ForumReportId);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.IsResolved).HasDefaultValue(false);

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Reports)
                .HasForeignKey(e => e.ForumTopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Comment)
                .WithMany()
                .HasForeignKey(e => e.ForumCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ReportedBy)
                .WithMany(u => u.ForumReports)
                .HasForeignKey(e => e.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResolvedBy)
                .WithMany(u => u.ForumResolvedReports)
                .HasForeignKey(e => e.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumTopicHistory>(entity =>
        {
            entity.ToTable("ForumTopicHistories");
            entity.HasKey(e => e.ForumTopicHistoryId);
            entity.Property(e => e.TitleSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContentSnapshot).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.ChangedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.Topic)
                .WithMany(t => t.Histories)
                .HasForeignKey(e => e.ForumTopicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumCommentHistory>(entity =>
        {
            entity.ToTable("ForumCommentHistories");
            entity.HasKey(e => e.ForumCommentHistoryId);
            entity.Property(e => e.ContentSnapshot).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.ChangedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.Comment)
                .WithMany(c => c.Histories)
                .HasForeignKey(e => e.ForumCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedBy)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ForumUserBan>(entity =>
        {
            entity.ToTable("ForumUserBans");
            entity.HasKey(e => e.ForumUserBanId);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.EndsAt).HasColumnType("datetime");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ForumBans)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BannedBy)
                .WithMany(u => u.ForumBansIssued)
                .HasForeignKey(e => e.BannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__Conversa__C050D89797404FD7");
            entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedConversations)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.ToTable("ConversationParticipants");
            entity.HasKey(e => e.ConversationParticipantId);
            entity.Property(e => e.JoinedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastReadAt).HasColumnType("datetime");
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ConversationParticipants)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Messages__C87C037C72BC4950");
            entity.Property(e => e.MessageId).HasColumnName("MessageID");
            entity.Property(e => e.ConversationId).HasColumnName("ConversationID");
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");
            entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AttachmentPath).HasMaxLength(255);
            entity.Property(e => e.AttachmentName).HasMaxLength(255);
            entity.Property(e => e.AttachmentContentType).HasMaxLength(100);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.ReadAt).HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Conver__656C112C");

            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Receiv__6754599E");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Messages__Sender__66603565");
        });

        modelBuilder.Entity<PrivateUserBlock>(entity =>
        {
            entity.ToTable("PrivateUserBlocks");
            entity.HasKey(e => e.PrivateUserBlockId);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.HasIndex(e => new { e.BlockerUserId, e.BlockedUserId }).IsUnique();

            entity.HasOne(e => e.Blocker)
                .WithMany(u => u.BlocksIssued)
                .HasForeignKey(e => e.BlockerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Blocked)
                .WithMany(u => u.BlocksReceived)
                .HasForeignKey(e => e.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrivateUserReport>(entity =>
        {
            entity.ToTable("PrivateUserReports");
            entity.HasKey(e => e.PrivateUserReportId);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.IsResolved).HasDefaultValue(false);

            entity.HasOne(e => e.Reporter)
                .WithMany(u => u.PrivateReportsIssued)
                .HasForeignKey(e => e.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Reported)
                .WithMany(u => u.PrivateReportsReceived)
                .HasForeignKey(e => e.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResolvedBy)
                .WithMany(u => u.PrivateReportsResolved)
                .HasForeignKey(e => e.ResolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MentorshipRequest>(entity =>
        {
            entity.ToTable("MentorshipRequests");
            entity.HasKey(e => e.MentorshipRequestId);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
            entity.HasIndex(e => new { e.StudentId, e.AlumniId }).IsUnique();

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Alumni)
                .WithMany()
                .HasForeignKey(e => e.AlumniId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
