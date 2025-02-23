﻿using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using ApplicationCore.Specifications.Subscriptions;
using ApplicationCore.Specifications.User;
using Web.Interfaces;
using Web.ViewModels;

namespace Web.Services
{
    public class SubscriptionViewModelService : ISubscriptionViewModelService
    {
        private readonly IRepository<User> _userrepository;
        private readonly IRepository<Subscription> _subscriptionrepository;
        private readonly IRepository<Book> _bookrepository;
        private readonly IUriComposer _uriComposer;
        public SubscriptionViewModelService(IRepository<User> userrepository,
                                            IRepository<Subscription> subscriptionrepository,
                                            IRepository<Book> bookrepository,
        IUriComposer uriComposer)
        {
            _userrepository = userrepository;
            _subscriptionrepository = subscriptionrepository;
            _bookrepository = bookrepository;
            _uriComposer = uriComposer;
        }

        public async Task AddSubscriptionAsync(string bookid, string userid)
        {
            var user = await EnsureUserExistsAsync(userid);

            //ensure subscription does not already exist
            var specification = new SubscriptionSpecification(userid, bookid);

            var subscription = await _subscriptionrepository.GetBySpecAsync(specification);

            if (subscription != null)
            {
                throw new Exception("Subscription already exists.");
            }

            await _subscriptionrepository.AddAsync(new Subscription(user.Email, bookid));
        }

        public async Task DeleteSubscriptionAsync(Guid subscriptionid)
        {
            var subscription = await _subscriptionrepository.GetByIdAsync(subscriptionid);

            if (subscription == null)
            {
                throw new Exception("Subscription does not exist.");
            }

            await _subscriptionrepository.DeleteAsync(subscription);

            await _subscriptionrepository.SaveChangesAsync();
        }

        public async Task<string> GetBookTextAsync(Guid subscriptionid, string userid)
        {
            var subscription = await _subscriptionrepository.GetByIdAsync(subscriptionid);

            if (subscription == null)
            {
                throw new Exception("Subscription no longer exists.");
            }

            var book = await _bookrepository.GetByIdAsync(subscription.BookIsbn);

            return book.Text;
        }

        public async Task<SubscriptionViewModel> GetUserSubscriptionViewModelAsync(string userid)
        {
            var subscriptionViewModel = new SubscriptionViewModel();

            var user = await EnsureUserExistsAsync(userid);

            var userWithSubsSpec = new UserWithSubscriptionSpecification(user.Email);

            var userWithSubs = await _userrepository.GetBySpecAsync(userWithSubsSpec);

            subscriptionViewModel.MonthlySubscription = "R" + userWithSubs.Subscriptions.Sum(s => s.Book.SubscriptionPrice).ToString();
            subscriptionViewModel.Subscriptions = userWithSubs.Subscriptions.Select(s =>
                                                   new SubscriptionItemViewModel
                                                   {
                                                       Id = s.Id,
                                                       Title = s.Book.Title,
                                                       Description = s.Book.Description,
                                                       PictureUri = _uriComposer.ComposePicUri(s.Book.PictureUri)
                                                   }).ToList();

            return subscriptionViewModel;
        }

        private async Task<User> EnsureUserExistsAsync(string userid)
        {
            var user = await _userrepository.GetByIdAsync(userid);

            if (user == null)
            {
                user = await _userrepository.AddAsync(new User(userid));
            }

            return user;
        }
    }
}
