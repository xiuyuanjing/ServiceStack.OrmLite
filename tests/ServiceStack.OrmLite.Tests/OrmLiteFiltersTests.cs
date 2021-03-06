﻿using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    public interface IAudit
    {
        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
    }

    public class AuditTableA : IAudit
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class AuditTableB : IAudit
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    [TestFixture]
    public class OrmLiteFiltersTests
        : OrmLiteTestBase
    {
        [Test]
        public void Does_call_Filters_on_insert_and_update()
        {
            var insertDate = new DateTime(2014, 1, 1);
            var updateDate = new DateTime(2015, 1, 1);

            OrmLiteConfig.InsertFilter = (dbCmd, row) =>
            {
                var auditRow = row as IAudit;
                if (auditRow != null)
                {
                    auditRow.CreatedDate = auditRow.ModifiedDate = insertDate;
                }
            };

            OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                var auditRow = row as IAudit;
                if (auditRow != null)
                {
                    auditRow.ModifiedDate = updateDate;
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                var idA = db.Insert(new AuditTableA(), selectIdentity: true);
                var idB = db.Insert(new AuditTableB(), selectIdentity: true);

                var insertRowA = db.SingleById<AuditTableA>(idA);
                var insertRowB = db.SingleById<AuditTableB>(idB);

                Assert.That(insertRowA.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(insertDate));

                Assert.That(insertRowB.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowB.ModifiedDate, Is.EqualTo(insertDate));

                insertRowA.ModifiedBy = "Updated";
                db.Update(insertRowA);

                insertRowA = db.SingleById<AuditTableA>(idA);
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(updateDate));
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

        [Test]
        public void Does_call_Filters_on_Save()
        {
            var insertDate = new DateTime(2014, 1, 1);
            var updateDate = new DateTime(2015, 1, 1);

            OrmLiteConfig.InsertFilter = (dbCmd, row) =>
            {
                var auditRow = row as IAudit;
                if (auditRow != null)
                {
                    auditRow.CreatedDate = auditRow.ModifiedDate = insertDate;
                }
            };

            OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                var auditRow = row as IAudit;
                if (auditRow != null)
                {
                    auditRow.ModifiedDate = updateDate;
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                var a = new AuditTableA();
                var b = new AuditTableB();
                db.Save(a);
                db.Save(b);

                var insertRowA = db.SingleById<AuditTableA>(a.Id);
                var insertRowB = db.SingleById<AuditTableB>(b.Id);

                Assert.That(insertRowA.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(insertDate));

                Assert.That(insertRowB.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowB.ModifiedDate, Is.EqualTo(insertDate));

                a.ModifiedBy = "Updated";
                db.Save(a);

                a = db.SingleById<AuditTableA>(a.Id);
                Assert.That(a.ModifiedDate, Is.EqualTo(updateDate));
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

        [Test]
        public void Exceptions_in_filters_prevents_insert_and_update()
        {
            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                var auditRow = row as IAudit;
                if (auditRow != null)
                {
                    if (auditRow.ModifiedBy == null)
                        throw new ArgumentNullException("ModifiedBy");
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                try
                {
                    db.Insert(new AuditTableA());
                    Assert.Fail("Should throw");
                }
                catch (ArgumentNullException) { }
                Assert.That(db.Count<AuditTableA>(), Is.EqualTo(0));

                var a = new AuditTableA { ModifiedBy = "Me!" };
                db.Insert(a);

                a.ModifiedBy = null;
                try
                {
                    db.Update(a);
                    Assert.Fail("Should throw");
                }
                catch (ArgumentNullException) { }

                a.ModifiedBy = "Me2!";
                db.Update(a);
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

    }
}