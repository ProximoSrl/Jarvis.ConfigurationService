(function (window, angular, undefined) {
    'use strict';

    angular
        .module('admin.layout')
        .directive('dsSidebar', dsSidebar);

    dsSidebar.$inject = ['dashboardData', 'configService'];

    function dsSidebar(dashboardData, configService) {
        var directive = {
            link: link,
            templateUrl: '/layout/sidebar.html',
            restrict: 'E',
            replace: true
        };

        return directive;

        function link(scope, element, attrs) {
            var vm = scope;
            vm.selectedService = '';
            vm.selectedApplication = '';
            vm.applications = [];

            vm.selectApp = function (app) {
                vm.selectedApplication = app;
            };

            configService.getStatus().then(function (res) {
                var status = res;
                return res;
            }).then(function (status) {
                angular.forEach(status.Applications, function (tname) {
                    var appStats = {
                        "name": tname
                    };
                    dashboardData.getApplicationStats(tname).then(function (d) {
                        appStats.services = d;
                        vm.applications.push(appStats);
                    });
                });
                console.log('APPS:LOADED::', vm.applications);
            });
        }
    };
})(window, window.angular);
