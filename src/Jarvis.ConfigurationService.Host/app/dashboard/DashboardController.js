(function (window, angular, undefined) {
    'use strict';

    angular.module('admin.dashboard').controller('DashboardController', DashboardController);

    DashboardController.$inject = ['$interval', '$scope', 'status', '$http', '$stateParams'];

    function DashboardController($interval, $scope, status, $http, $stateParams) {

        var vm = this;
        vm.version = status.Version;
        vm.fileVersion = status.FileVersion;
        vm.informationalVersion = status.InformationalVersion;
        //vm.applications = [];
        vm.currentApplication = $stateParams.application;
        vm.currentService = $stateParams.service;
        vm.appDetailsVisible = false;
        /* */
        console.log('LOAD_VM', vm);

        var update = function () {

        };

        vm.currentEditedService;
        vm.editConfig = function (service) {
            if (!service) return;
            var url = '/' + vm.currentApplication + '/' + service + '.config?missingParametersAction=ignore';
            vm.currentEditedService = service;
            $http({
                method: 'GET',
                url: url
            }).then(
                function successCallback(response) {
                    vm.currentConfig = response.data;
                },
                function errorCallback(error) {
                    vm.currentConfig = error.data
                }
            );

            var urlParameter = '/api/parameters/' + vm.currentApplication;

            $http({
                method: 'GET',
                url: urlParameter
            }).then(
                function successCallback(response) {
                    vm.currentApplicationParameters = JSON.stringify(response.data, null, 4);
                    vm.currentApplicationParametersOperation = { "operation": "load", "status": "OK" };
                },
                function errorCallback(error) {
                    vm.currentApplicationParameters = JSON.stringify(error.data, null, 4);
                    vm.currentApplicationParametersOperation = { "operation": "load", "status": "Error loading parameter" };
                }
            );
        };
        vm.editConfig(vm.currentService);

        vm.saveCurrentParameters = function () {
            if (!vm.currentApplication) return;
            if (!vm.currentApplicationParameters) return;
            var urlParameter = '/api/parameters/' + vm.currentApplication;
            vm.currentApplicationParametersOperation = { "operation": "save", "status": "pending" };
            $http({
                method: 'PUT',
                url: urlParameter,
                data: vm.currentApplicationParameters
            }).then(function successCallback(response) {
                debugger;
                vm.currentApplicationParametersOperation = { "operation": "save", "status": response.data.success ? "OK" : "KO" };
                if (response.data.success) {
                    vm.editConfig(vm.currentEditedService);
                }
            });
        };

        $scope.$on('$destroy', function () {
            $interval.cancel(stop);
        });


        var stop = $interval(update, 60 * 1000);
    }
})(window, window.angular);
