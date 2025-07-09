using System;
using System.Collections.Generic;
using System.Linq;
using Library.ApplicationCore.Entities;
using Xunit;

namespace UnitTests.ApplicationCore
{
    public class BookAvailabilityTests
    {
        [Fact]
        public void BookIsAvailable_WhenNoActiveLoan_ReturnsTrue()
        {
            // Arrange
            var book = new Book { Id = 1, Title = "Test Book", AuthorId = 1, Genre = "Test", ImageName = "img.jpg", ISBN = "123" };
            var bookItem = new BookItem { Id = 1, BookId = 1, AcquisitionDate = DateTime.Now, Condition = "Good", Book = book };
            var loans = new List<Loan>();

            // Act
            bool isAvailable = !loans.Any(l => l.BookItemId == bookItem.Id && l.ReturnDate == null);

            // Assert
            Assert.True(isAvailable);
        }

        [Fact]
        public void BookIsNotAvailable_WhenActiveLoanExists_ReturnsFalse()
        {
            // Arrange
            var book = new Book { Id = 2, Title = "Test Book 2", AuthorId = 2, Genre = "Test", ImageName = "img2.jpg", ISBN = "456" };
            var bookItem = new BookItem { Id = 2, BookId = 2, AcquisitionDate = DateTime.Now, Condition = "Fair", Book = book };
            var loans = new List<Loan>
            {
                new Loan { Id = 1, BookItemId = 2, PatronId = 1, LoanDate = DateTime.Now, DueDate = DateTime.Now.AddDays(14), ReturnDate = null }
            };

            // Act
            bool isAvailable = !loans.Any(l => l.BookItemId == bookItem.Id && l.ReturnDate == null);

            // Assert
            Assert.False(isAvailable);
        }
    }
}
