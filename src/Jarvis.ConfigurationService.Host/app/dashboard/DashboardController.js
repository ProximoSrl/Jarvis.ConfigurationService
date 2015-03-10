(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('DashboardController', DashboardController);

    DashboardController.$inject = ['dashboardData', '$interval', '$scope', 'status'];

    function DashboardController(dashboardData, $interval, $scope, status) {

        console.log('Applications are ', status);

        var vm = this;
        vm.title = 'prova';
        vm.version = status.Version;
        vm.informationalVersion = status.InformationalVersion;
        vm.applications = [];
        /* */

        var init = function () {

            angular.forEach(status.Applications, function (tname) {
                var appStats = {
                    "name": tname
                };
                dashboardData.getApplicationStats(tname).then(function (d) {
                    appStats.stats = d;
                    vm.applications.push(appStats);
                });
            });

            update();
        }

        var update = function () {
            
        };

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });

        init();

        var stop = $interval(update, 60 * 1000);
    }
})(window, window.angular);
