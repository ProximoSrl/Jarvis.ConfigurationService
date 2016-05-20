(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('DashboardController', DashboardController);

    DashboardController.$inject = ['dashboardData', '$interval', '$scope', 'status', '$http'];

    function DashboardController(dashboardData, $interval, $scope, status, $http) {

        console.log('Applications are ', status);
   
        var vm = this;
        vm.title = 'Dashboard';
        vm.version = status.Version;
        vm.fileVersion = status.FileVersion;
        vm.informationalVersion = status.InformationalVersion;
        vm.applications = [];
        vm.appDetailsVisible = false;
        /* */

        var init = function () {

            angular.forEach(status.Applications, function (tname) {
                var appStats = {
                    "name": tname
                };
                dashboardData.getApplicationStats(tname).then(function (d) {
                    appStats.services = d;
                    vm.applications.push(appStats);
                });
            });

            update();
        }

        var update = function () {
            
        };

        vm.showApp = function (app)
        {
            vm.currentApplication = app;
            vm.appDetailsVisible = true;
        }

        vm.editConfig = function (service) {
            var url = '/' + vm.currentApplication.name + '/' + service + '.config?missingParametersAction=ignore';

            $http({
                method: 'GET',
                url: url
            }).then(function successCallback(response) {
           
                vm.currentConfig = response.data; 
            });

        };

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });



        init();

        var stop = $interval(update, 60 * 1000);
    }
})(window, window.angular);
